using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Web.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Owin;

[assembly: OwinStartup(typeof(APIs.Startup2))]  // Indica a OWIN dónde arrancar

namespace APIs
{
    public class Startup2
    {
        public void Configuration(IAppBuilder app)
        {
            var issuer = ConfigurationManager.AppSettings["JWT:Issuer"];
            var secretKey = ConfigurationManager.AppSettings["JWT:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            JwtBearerAuthenticationOptions options = new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = JwtRegisteredClaimNames.Sub
                },
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        // Verifica si el token llega
                        System.Diagnostics.Debug.WriteLine("Token recibido: " + context.Ticket.Identity.Name);
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                }
            };

            app.UseJwtBearerAuthentication(options);

            // Configuración de Web API
            HttpConfiguration config = new HttpConfiguration();

            // Rutas por atributos (ej: [Route("api/values")])
            config.MapHttpAttributeRoutes();

            // Ruta por defecto
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Activar autenticación JWT aquí si la usas
            JwtConfig.ConfigureAuth(app);

            app.UseWebApi(config);
        }
    }
}
