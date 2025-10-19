using Entities.Request;
using Entities.Response;
using Logica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;


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

        [Authorize(Roles = "Profesional")]
        [HttpPost]
        [Route("api/Servicios/CrearServicio")]
        public ResCrearServicio CrearServicio(ReqCrearServicio req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogServicio().CrearServicio(req, token);
        }
        [Authorize(Roles = "Profesional")]
        [HttpPost]
        [Route("api/Servicios/ActualizarServicio")]
        public ResActualizarServicio ActualizarServicio(ReqActualizarServicio req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogServicio().ActualizarServicio(req, token);
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

        [Authorize]
        [HttpPost]
        [Route("api/SolicitarCita")]
        public ResSolicitarCita SolicitarCita([FromBody] ReqSolicitarCita req)
        {
            // Se extrae el token del encabezado Authorization
            var token = Request.Headers.Authorization?.Parameter;

            return new LogSolicitarCita().SolicitarCita(req, token);
        }

        [Authorize]
        [HttpPost]
        [Route("api/AprobarDenegarCita")]
        public ResActualizarEstadoCita AprobarDenegarCita([FromBody] ReqActualizarEstadoCita req)
        {
            var token = Request.Headers.Authorization?.Parameter;

            return new LogCita().ActualizarEstadoCita(req, token);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [Route("api/ListarServiciosFiltros")]
        public ResListarServiciosParaClientes ListarServicios([FromBody]ReqListarServiciosParaClientes req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogServicio().ListarServiciosParaClientes(req, token);
        }

        [Authorize]
        [HttpPost]
        [Route("api/ReprogramarCita")]
        public ResReprogramarCita ReprogramarCita([FromBody] ReqReprogramarCita req)
        {
            // Se extrae el token del encabezado Authorization
            var token = Request.Headers.Authorization?.Parameter;

            return new LogReprogramarCita().ReprogramarCita(req, token);
        }

        [Authorize]
        [HttpPost]
        [Route("api/AgregarHorarios")]
        public ResAgregarHorarioProfesional AgregarHorarioProfesional([FromBody] ReqAgregarHorarioProfesional req)
        {
           
            var token = Request.Headers.Authorization?.Parameter;

            return new LogHorarioProfesional().AgregarHorarios(req, token);
        }

        [Authorize]
        [HttpPost]
        [Route("api/CrearEvento")]
        public ResCrearEvento CrearEvento([FromBody] ReqCrearEvento req)
        {

            var token = Request.Headers.Authorization?.Parameter;

            return new LogHorarioProfesional().CrearEvento(req, token);
        }


        [Authorize]
        [HttpPost]
        [Route("api/obtenerPorcentajeCancelacion")]
        public ResMetricasCancelacion porcentajeCancelacion()
        {

            var token = Request.Headers.Authorization?.Parameter;

            return new LogMetricasCancelacion().ObtenerMetricasCancelacion(token);
        }
    }
}