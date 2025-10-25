using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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

        /// <summary>
        /// Genera un token JWT para un usuario
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <param name="rol">Rol del usuario (Cliente/Profesional)</param>
        /// <param name="idPlan">ID del plan (solo para profesionales, null para clientes)</param>
        public static string GenerateToken(int idUsuario, string rol, int? idPlan = null)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                new Claim(ClaimTypes.Role, rol ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Si el rol es Profesional y tiene un idPlan, agregarlo al token
            if (!string.IsNullOrEmpty(rol) && rol.Equals("Profesional", StringComparison.OrdinalIgnoreCase) && idPlan.HasValue)
            {
                claims.Add(new Claim("IdPlan", idPlan.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Issuer,
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

        public static int? GetUserIdFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);

                var idClaim = principal.Claims.FirstOrDefault(c =>
                    c.Type == JwtRegisteredClaimNames.Sub ||
                    c.Type == ClaimTypes.NameIdentifier);

                if (idClaim != null && int.TryParse(idClaim.Value, out int idUsuario))
                    return idUsuario;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el IdPlan desde el token (solo para profesionales)
        /// </summary>
        public static int? GetPlanIdFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
               
                var planClaim = principal.Claims.FirstOrDefault(c => c.Type == "IdPlan");

                if (planClaim != null && int.TryParse(planClaim.Value, out int idPlan))
                    return idPlan;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el rol desde el token
        /// </summary>
        public static string GetRoleFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                return roleClaim?.Value;
            }
            catch
            {
                return null;
            }
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