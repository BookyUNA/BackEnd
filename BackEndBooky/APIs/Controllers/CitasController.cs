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
    public class CitasController : ApiController
    {
        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [Route("api/ListarCitasCliente")]
        public ResListarCitasCliente ListarCitasCliente([FromBody] ReqListarCitasCliente req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogCita().ListarCitasCliente(req, token);
        }

        [Authorize(Roles = "Profesional")]
        [HttpPost]
        [Route("api/ListarCitasProfesional")]
        public ResListarCitasProfesional ListarCitasProfesional([FromBody] ReqListarCitasProfesional req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogCita().ListarCitasProfesional(req, token);
        }
    }
}