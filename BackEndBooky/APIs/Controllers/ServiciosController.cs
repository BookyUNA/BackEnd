using Entities.Request;
using Entities.Response;
using Logica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;


namespace APIs.Controllers
{
    [Authorize]
    public class ServiciosController : ApiController
    {
        // GET: Servicios
        [Authorize (Roles = "Profesional")]
        [HttpPost]
        [Route("api/Servicios/ListarServiciosProfesional")]
        public ResListarServicio ListarServicios (ReqListarServicio req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogServicio().ListarServicios(req, token);
        }

        [Authorize]
        [HttpPost]
        [Route("api/CambiarEstadoServicio")]
        public ResActivarDesactivarServicio CambiarEstadoServicio([FromBody] ReqActivarDesactivarServicio req)
        {
            // Se extrae el token del encabezado Authorization
            var token = Request.Headers.Authorization?.Parameter;

            return new LogActivarDesactivarServicio().ActivarDesactivarServicio(req, token);
        }
    }
}