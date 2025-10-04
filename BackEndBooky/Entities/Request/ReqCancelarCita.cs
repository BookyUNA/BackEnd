using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqCancelarCita
    {
        public int IdCita { get; set; }
        public string MotivoCancelacion { get; set; }
    }
}
