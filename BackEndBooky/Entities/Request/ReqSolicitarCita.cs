using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqSolicitarCita
    {
        public int IdServicio { get; set; }
        public DateTime FechaCita { get; set; }
        public string MensajeSolicitud { get; set; }
    }
}
