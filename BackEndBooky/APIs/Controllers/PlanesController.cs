using Entities.Request;
using Entities.Response;
using Logica;
using System.Web.Http;

namespace APIs.Controllers
{
    [Authorize]
    public class PlanesController : ApiController
    {
        /// <summary>
        /// Asigna un plan a un perfil profesional
        /// </summary>
        [Authorize(Roles = "Profesional")]
        [HttpPost]
        [Route("api/Planes/AsignarPlan")]
        public ResAsignarPlan AsignarPlan([FromBody] ReqAsignarPlan req)
        {
            var token = Request.Headers.Authorization?.Parameter;
            return new LogPlanes().AsignarPlan(req, token);
        }

        /// <summary>
        /// Obtiene el plan actual del profesional autenticado
        /// </summary>
        [Authorize(Roles = "Profesional")]
        [HttpPost]
        [Route("api/Planes/ObtenerPlan")]
        public ResObtenerPlan ObtenerPlan()
        {
            var token = Request.Headers.Authorization?.Parameter;
            return new LogPlanes().ObtenerPlan(token);
        }

      




    }
}