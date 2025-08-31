using Entities.Request;
using Entities.Response;
using Logica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace APIs.Controllers
{
    [Authorize]
    public class ServiciosController : Controller
    {
        // GET: Servicios
        [Authorize (Roles = "Profesional")]
        [HttpPost]
        public ResListarServicio ListarServicios (ReqListarServicio req)
        {
            string token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return new LogServicios.ListarServicios(req, token);
        }
    }
}