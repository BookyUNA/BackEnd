using Owin;
using System.Configuration;
using System.Text;
using System;
using Microsoft.Owin.Security.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class JwtConfig
{
    public static void ConfigureAuth(IAppBuilder app)
    {
        var issuer = ConfigurationManager.AppSettings["JWT:Issuer"];
        var secretKey = ConfigurationManager.AppSettings["JWT:SecretKey"];

        var key = Encoding.ASCII.GetBytes(secretKey);

        app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
        {
            AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
            TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Para que la expiración del token sea más estricta
            }
        });
    }

    public static string GenerarToken(int idUsuario, string rol)
    {
        var issuer = ConfigurationManager.AppSettings["JWT:Issuer"];
        var secretKey = ConfigurationManager.AppSettings["JWT:SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

        var claims = new[]
        {
                new Claim("IdUsuario", idUsuario.ToString()),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // identificador único del token
            };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
