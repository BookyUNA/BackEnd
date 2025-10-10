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
    public class ProfesionalController : ApiController
    {
        [Authorize]
        [HttpPost]
        [Route("api/ObtenerCalficacionPromedio")]
        public ResCalificacionPromedio ObtenerCalificacionPromedioProfesional([FromBody] ReqCalificacionPromedio req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogPerfilProfesional().ObtenerCalificacionPromedio(req, token);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [Route("api/CalificarProfesional")]
        public ResCalificarProfesional CalificarProfesional([FromBody] ReqCalificarProfesional req)
        {
            var token = Request.Headers.Authorization.Parameter;
            return new LogPerfilProfesional().CalificarProfesional(req, token);
        }
    }
}