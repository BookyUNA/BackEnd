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
using Unity;
using Unity.Lifetime;
using Unity.WebApi;
using Unity.Injection;
using Entities.Entity;
using Logica;
using Logica.Service;

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

            // CONFIGURAR UNITY AQUÍ
            ConfigureDependencyInjection(config);

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

        private void ConfigureDependencyInjection(HttpConfiguration config)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Configuring Unity in Startup2...");

                var container = new UnityContainer();

                // Registrar la configuración de Onvo
                var onvoConfig = new OnvoConfiguration
                {
                    BaseUrl = ConfigurationManager.AppSettings["OnvoBaseUrl"] ?? "https://api.onvopay.com/v1",
                    SecretKey = ConfigurationManager.AppSettings["OnvoSecretKey"] ?? "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg",
                    TimeoutSeconds = int.Parse(ConfigurationManager.AppSettings["OnvoTimeoutSeconds"] ?? "30")
                };

                // Registrar como instancia singleton
                container.RegisterInstance<OnvoConfiguration>(onvoConfig);

                // Registrar servicios con lifetime manager adecuado
                // CAMBIO: Usar el servicio sincrónico seguro
                container.RegisterType<IOnvoPaymentService, OnvoPaymentService>(new ContainerControlledLifetimeManager());

                // Registrar LogOnvoPayment especificando el constructor con parámetros
                container.RegisterType<LogOnvoPayment>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(typeof(IOnvoPaymentService))
                );

                // Registrar el controlador explícitamente
                container.RegisterType<APIs.Controllers.OnvoController>(
                    new HierarchicalLifetimeManager(),
                    new InjectionConstructor(typeof(LogOnvoPayment))
                );

                // Configurar el resolver de dependencias EN LA CONFIGURACIÓN CORRECTA
                config.DependencyResolver = new UnityDependencyResolver(container);

                System.Diagnostics.Debug.WriteLine("Unity configured successfully in Startup2");

                // Verificar que las dependencias se pueden resolver correctamente
                var testService = container.Resolve<IOnvoPaymentService>();
                var testLog = container.Resolve<LogOnvoPayment>();

                System.Diagnostics.Debug.WriteLine($"Services resolved successfully: Service={testService != null}, Log={testLog != null}");

                // Verificar que LogOnvoPayment tiene el servicio inyectado
                System.Diagnostics.Debug.WriteLine($"LogOnvoPayment has service: {testLog?.HasService() == true}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring Unity in Startup2: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}