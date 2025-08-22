using Entities.Request;
using Entities.Response;
using Logic.Helpers;
using Logica;
using System;
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

        [HttpPost]
        [Route("api/GenerarNuevoCodigoRecuperacion")]
        public ResGenerarNuevoCodigo GenerarCodigoRecuperacion([FromBody] ReqGenerarNuevoCodigo req)
        {
            return new LogGenerarNuevoCodigo().GenerarNuevoCodigo(req);
        }
       
        [HttpPost]
        [Route("api/RegistrarUsuario")]
        public ResAgregarUsuario RegistrarUsuario([FromBody] ReqAgregarUsuario req)
        {
            return new LogUsuario().RegistrarUsuario(req);
        }

        [HttpPost]
        [Route("api/VerificarEmail")]
        public ResVerificarEmail VerificarEmail([FromBody] ReqVerificarEmail req)
        {
            return new LogUsuario().VerificarEmail(req);
        }


        [HttpPost]
        [Route("api/GenerarNuevoCodigoVerificacion")]
        public ResGenerarNuevoCodigo GenerarNuevoCodigoVerificacion([FromBody] ReqGenerarNuevoCodigo req)
        {
            return new LogUsuario().GenerarCodigoVerificacion(req);
        }
    }
}
