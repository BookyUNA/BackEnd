using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResRegistrarPagoOnvo : ResBase
    {


        public int IdPagoOnvo { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Estado { get; set; } 
        public string MensajeEstado { get; set; }
        public DateTime FechaCreacion { get; set; }
     
    }
}
