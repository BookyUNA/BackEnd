using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{

    public class ResOnvoFlowCompleto : ResBase
    {
        public bool resultado { get; set; }
        public string mensaje { get; set; }

        // Cliente
        public string customerId { get; set; }

        // Tarjeta
        public string paymentMethodId { get; set; }
        public string last4 { get; set; }
        public string brand { get; set; }

        // URL de pago
        public string paymentLinkId { get; set; }
        public string paymentUrl { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }


    }

}
