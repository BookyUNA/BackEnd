using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqListarServiciosParaClientes
    {
        public string nombreServicio { get; set; } = string.Empty;
        public string nombreProfesional { get; set; } = string.Empty;

        public string profesion { get; set; } = string.Empty;
    }
}
