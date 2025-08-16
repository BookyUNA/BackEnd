using Microsoft.Owin;
using Owin;
using System.Web.Http;

[assembly: OwinStartup(typeof(APIs.Startup))]

namespace APIs
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            // Configurar rutas API
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = System.Web.Http.RouteParameter.Optional }
            );

            // Configurar autenticación JWT
            JwtConfig.ConfigureAuth(app);

            // Registrar Web API para que OWIN lo use
            app.UseWebApi(config);
        }
    }
}
