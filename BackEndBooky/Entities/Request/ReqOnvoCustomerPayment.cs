using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqOnvoFlowCompleto
    {
        // Datos del cliente
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }

        // Datos de la tarjeta
        public string CardNumber { get; set; }
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string Cvc { get; set; }
        public string CardholderName { get; set; }

        // Datos del pago
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
