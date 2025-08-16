using System;
using System.Configuration;
using System.Text;
using Microsoft.Owin.Security.Jwt;
using Microsoft.IdentityModel.Tokens;
using Owin;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace APIs
{
    public static class JwtConfig
    {
        public static void ConfigureAuth(IAppBuilder app)
        {
            var issuer = ConfigurationManager.AppSettings["JWT:Issuer"];
            var secretKey = ConfigurationManager.AppSettings["JWT:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            var options = new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = false, // sin audience
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = JwtRegisteredClaimNames.Sub
                }
            };

            app.UseJwtBearerAuthentication(options);
        }
    }
}
