using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Logica.Service;
using Logica;
using Unity;
using Unity.Lifetime;
using Unity.WebApi;
using Unity.Injection;
using Entities.Entity;
using System.Configuration;
using APIs.Controllers;

namespace APIs
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            log4net.Config.XmlConfigurator.Configure();
            ConfigureDependencyInjection();
        }

        private void ConfigureDependencyInjection()
        {
            try
            {
                var container = new UnityContainer();

                // Registrar la configuración de Onvo
                var onvoConfig = new OnvoConfiguration
                {
                    BaseUrl = ConfigurationManager.AppSettings["OnvoBaseUrl"] ?? "https://api.onvopay.com/v1",
                    SecretKey = ConfigurationManager.AppSettings["OnvoSecretKey"] ?? "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg",
                    TimeoutSeconds = int.Parse(ConfigurationManager.AppSettings["OnvoTimeoutSeconds"] ?? "30")
                };

                // Registrar como instancia singleton
                container.RegisterInstance<OnvoConfiguration>(onvoConfig);

                // Registrar servicios con PerResolve lifetime
                container.RegisterType<IOnvoPaymentService, OnvoPaymentService>(new ContainerControlledLifetimeManager());
                container.RegisterType<LogOnvoPayment>(new ContainerControlledLifetimeManager());

                // IMPORTANTE: Registrar explícitamente el controlador
                container.RegisterType<OnvoController>(new HierarchicalLifetimeManager());

                // Configurar el resolver de dependencias
                GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);

                System.Diagnostics.Debug.WriteLine("Dependency injection configured successfully");

                // Verificar que las dependencias se pueden resolver
                var testService = container.Resolve<IOnvoPaymentService>();
                var testLog = container.Resolve<LogOnvoPayment>();

                System.Diagnostics.Debug.WriteLine($"Services resolved successfully: Service={testService != null}, Log={testLog != null}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring dependency injection: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}