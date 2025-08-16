using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Logic
{
    public static class JwtService
    {
        private static readonly string SecretKey = ConfigurationManager.AppSettings["JWT:SecretKey"] ?? throw new ArgumentNullException("JWT:SecretKey no está configurado en Web.config");
        private static readonly string Issuer = ConfigurationManager.AppSettings["JWT:Issuer"] ?? throw new ArgumentNullException("JWT:Issuer no está configurado en Web.config");
        private static readonly string Audience = ConfigurationManager.AppSettings["JWT:Audience"] ?? Issuer;
        private static readonly double AccessTokenExpirationHours = Convert.ToDouble(ConfigurationManager.AppSettings["JWT:AccessTokenExpirationHours"] ?? "1");

        // Cache en memoria para tokens invalidados (key = jti)
        private static readonly MemoryCache _blacklistCache = MemoryCache.Default;

        /// <summary>
        /// Genera un token JWT con IdUsuario, Rol y un JTI único
        /// </summary>
        public static string GenerateToken(int idUsuario, string rol)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string jti = Guid.NewGuid().ToString(); // identificador único del token

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                new Claim(ClaimTypes.Role, rol ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var expiration = DateTime.UtcNow.AddHours(AccessTokenExpirationHours);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Obtiene el IdUsuario desde un token
        /// </summary>
        public static int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            if (subClaim == null || !int.TryParse(subClaim.Value, out int idUsuario))
                throw new SecurityTokenException("Token no contiene un IdUsuario válido");

            return idUsuario;
        }

        /// <summary>
        /// Obtiene el Rol desde un token
        /// </summary>
        public static string GetRoleFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            return roleClaim?.Value ?? string.Empty;
        }

        /// <summary>
        /// Invalida un token añadiendo su JTI a la blacklist hasta que expire
        /// </summary>
        public static void InvalidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var expUnix = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

            if (string.IsNullOrEmpty(jti))
                throw new SecurityTokenException("El token no contiene un identificador único (jti)");

            // Calcular expiración real
            DateTimeOffset expiration;
            if (!string.IsNullOrEmpty(expUnix) && long.TryParse(expUnix, out var expSeconds))
            {
                expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            }
            else
            {
                expiration = DateTimeOffset.UtcNow.AddHours(AccessTokenExpirationHours);
            }

            _blacklistCache.Add(jti, true, expiration);
        }

        /// <summary>
        /// Verifica si un token está invalidado
        /// </summary>
        public static bool IsTokenBlacklisted(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
                return false;

            return _blacklistCache.Contains(jti);
        }
    }
}
