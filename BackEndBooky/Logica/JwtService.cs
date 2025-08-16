using System;
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

        private static readonly IMemoryCache _blacklistCache = new MemoryCache(new MemoryCacheOptions());

        public static string GenerateToken(int idUsuario, string rol)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                new Claim(ClaimTypes.Role, rol ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: null, // sin audience
                claims: claims,
                expires: DateTime.UtcNow.AddHours(AccessTokenExpirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal ValidateToken(string token)
        {
            if (IsTokenBlacklisted(token))
                throw new SecurityTokenException("Token invalido");

            var handler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,

                ValidateAudience = false,
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

                return !string.IsNullOrEmpty(jti) && _blacklistCache.TryGetValue(jti, out _);
            }
            catch
            {
                return true;
            }
        }
    }
}
