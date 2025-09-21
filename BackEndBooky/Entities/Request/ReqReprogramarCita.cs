using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqReprogramarCita
    {
        public int IdCita { get; set; }
        public DateTime NuevaFechaCita { get; set; }
    }
}
