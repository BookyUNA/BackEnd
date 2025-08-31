using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqCambioDeContrasena
    {
        public string CodigoRecuperacion { get; set; }
        public string NuevaContrasenaHash { get; set; }
        public string ConfirmacionContrasenaHash { get; set; }
    }
}
