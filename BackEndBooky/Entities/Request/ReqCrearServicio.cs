using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqCrearServicio
    {
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public int duracionMinutos { get; set; }
        public decimal precio { get; set; }
        public bool permiteDescuento { get; set; }
        public decimal porcentajeDescuento { get; set; }
    }
}
