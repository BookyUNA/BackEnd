using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Logic
{
    public static class JwtService
    {
        
        
            private static readonly string SecretKey = ConfigurationManager.AppSettings["JWT:SecretKey"]
                ?? throw new ArgumentNullException("JWT:SecretKey no configurado");
            private static readonly string Issuer = ConfigurationManager.AppSettings["JWT:Issuer"]
                ?? throw new ArgumentNullException("JWT:Issuer no configurado");
            private static readonly double AccessTokenExpirationHours = Convert.ToDouble(
                ConfigurationManager.AppSettings["JWT:AccessTokenExpirationHours"] ?? "1");

            // Usar ConcurrentDictionary en lugar de IMemoryCache
            private static readonly ConcurrentDictionary<string, DateTime> _blacklistCache = new ConcurrentDictionary<string, DateTime>();

            public static string GenerateToken(int idUsuario, string rol)
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                new Claim(ClaimTypes.Role, rol ?? string.Empty), // Usar ClaimTypes.Role
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

                var token = new JwtSecurityToken(
                    issuer: Issuer,
                    audience: Issuer, // Usar el mismo issuer como audience
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(AccessTokenExpirationHours),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            public static ClaimsPrincipal ValidateToken(string token)
            {
                if (IsTokenBlacklisted(token))
                    throw new SecurityTokenException("Token invalidado");

                var handler = new JwtSecurityTokenHandler();
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };

                return handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            }

            public static bool IsTokenBlacklisted(string token)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (string.IsNullOrEmpty(jti))
                        return false;

                    // Verificar si existe y no ha expirado
                    if (_blacklistCache.TryGetValue(jti, out DateTime expiry))
                    {
                        if (DateTime.UtcNow > expiry)
                        {
                            // Remover tokens expirados
                            _blacklistCache.TryRemove(jti, out _);
                            return false;
                        }
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return true;
                }
            }

            public static void BlacklistToken(string token)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        // Guardar hasta la expiración del token
                        var expiry = jwtToken.ValidTo;
                        _blacklistCache.TryAdd(jti, expiry);
                    }
                }
                catch (Exception ex)
                {
                    // Log del error
                    System.Diagnostics.Debug.WriteLine($"Error blacklisting token: {ex.Message}");
                }
            }

            // Método para limpiar tokens expirados (llamar periódicamente)
            public static void CleanExpiredTokens()
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _blacklistCache
                    .Where(kvp => now > kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _blacklistCache.TryRemove(key, out _);
                }
            }
        
    }




}
