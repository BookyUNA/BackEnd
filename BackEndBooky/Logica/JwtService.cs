using System;
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
        private static readonly string SecretKey = ConfigurationManager.AppSettings["JWT:SecretKey"] ?? throw new ArgumentNullException("JWT:SecretKey no está configurado en Web.config");
        private static readonly string Issuer = ConfigurationManager.AppSettings["JWT:Issuer"] ?? throw new ArgumentNullException("JWT:Issuer no está configurado en Web.config");
        private static readonly string Audience = ConfigurationManager.AppSettings["JWT:Audience"] ?? Issuer;
        private static readonly double AccessTokenExpirationHours = Convert.ToDouble(ConfigurationManager.AppSettings["JWT:AccessTokenExpirationHours"] ?? "1");

        /// <summary>
        /// Genera un token JWT con solo IdUsuario y Rol
        /// </summary>
        public static string GenerateToken(int idUsuario, string rol)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()), // ID Usuario
                new Claim(ClaimTypes.Role, rol ?? string.Empty)               // Rol
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(AccessTokenExpirationHours),
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
    }
}
