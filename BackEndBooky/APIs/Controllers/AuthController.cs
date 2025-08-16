using Entities.Request;
using Entities.Response;
using Logica;

using System.Web.Http;

namespace API.Controllers
{
    public class LoginController : ApiController
    {
        [HttpPost]
        [Route("api/Login")]
        public ResInicioSesion IniciarSesion([FromBody] ReqInicioSesion req)
        {
            return new LogInicioSesion().LoginUsuario(req);
        }
    }
}
