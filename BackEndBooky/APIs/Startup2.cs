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
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(APIs.Startup2))]
namespace APIs
{
    public class Startup2
    {
        public void Configuration(IAppBuilder app)
        {
            // Configurar CORS si es necesario
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            // Configurar JWT ANTES de Web API
            ConfigureJwtAuthentication(app);

            // Configurar Web API
            ConfigureWebApi(app);
        }

        private void ConfigureJwtAuthentication(IAppBuilder app)
        {
            var issuer = ConfigurationManager.AppSettings["JWT:Issuer"];
            var secretKey = ConfigurationManager.AppSettings["JWT:SecretKey"];

            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT configuration missing");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);

            var options = new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                AllowedAudiences = new[] { issuer },
                IssuerSecurityKeyProviders = new[]
                {
                    new SymmetricKeyIssuerSecurityKeyProvider(issuer, key)
                },
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        try
                        {
                            // Extraer token del header
                            var authHeader = context.Request.Headers["Authorization"];
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                            {
                                var token = authHeader.Substring("Bearer ".Length).Trim();

                                // Verificar blacklist
                                if (Logic.JwtService.IsTokenBlacklisted(token))
                                {
                                    context.Rejected();
                                    return Task.FromResult(0);
                                }

                                System.Diagnostics.Debug.WriteLine($"Token válido para: {context.Ticket?.Identity?.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error validating token: {ex.Message}");
                            context.Rejected();
                        }

                        return Task.FromResult(0);
                    }
                }
            };

            app.UseJwtBearerAuthentication(options);
        }

        private void ConfigureWebApi(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            // Mapear rutas por atributos PRIMERO
            config.MapHttpAttributeRoutes();

            // Ruta por defecto
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Configurar formatters JSON
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            app.UseWebApi(config);
        }
    }
}
