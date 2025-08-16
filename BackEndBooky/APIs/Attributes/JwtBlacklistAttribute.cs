using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using Logic;

namespace APIs.Attributes
{
    public class JwtBlacklistAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // Primero ejecuta la autorización base
            base.OnAuthorization(actionContext);

            // Si ya falló la autorización base, no continuar
            if (actionContext.Response != null)
                return;

            // Obtener el token del header
            var authHeader = actionContext.Request.Headers.Authorization;
            if (authHeader != null && authHeader.Scheme == "Bearer")
            {
                var token = authHeader.Parameter;

                try
                {
                    // Verificar si está en blacklist
                    if (JwtService.IsTokenBlacklisted(token))
                    {
                        actionContext.Response = actionContext.Request.CreateResponse(
                            HttpStatusCode.Unauthorized,
                            new { message = "Token has been revoked" });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.Unauthorized,
                        new { message = "Invalid token format" });
                    return;
                }
            }
        }
    }
}