using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvoHistorialPagos
    {  public bool resultado { get; set; }
        public string mensaje { get; set; }
        public int totalRegistros { get; set; }
        public List<OnvoPaymentLinkDetalle> pagos { get; set; }
        public List<Error> error { get; set; }
    }
}
