using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvoResumenPagos : ResBase
    {

        public int totalPagos { get; set; }
        public int pagosCompletados { get; set; }
        public int pagosPendientes { get; set; }
        public int pagosCancelados { get; set; }
        public int pagosExpirados { get; set; }
        public decimal totalPagado { get; set; }
        public decimal totalPendiente { get; set; }
        public string currency { get; set; }
    }
}
