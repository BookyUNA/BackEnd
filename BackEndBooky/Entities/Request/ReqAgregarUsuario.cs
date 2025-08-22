using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqAgregarUsuario
    {
        public string nombreCompleto {  get; set; }
        public string cedula { get; set; }

        public string email { get; set; }

        public string telefono { get; set; }

        public string rol { get; set; }

        public string password { get; set; }

        

    }
}
