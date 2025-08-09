using Owin;
using System.Configuration;
using System.Text;
using System;
using Microsoft.Owin.Security.Jwt;
using Microsoft.IdentityModel.Tokens;

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
}
