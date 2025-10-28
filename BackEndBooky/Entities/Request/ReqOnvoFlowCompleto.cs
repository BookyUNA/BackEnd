using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqOnvoFlowCompleto
    {
        // Datos del Cliente
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }

        // Datos de la Tarjeta
        public object CardNumber { get; set; }  // Puede ser string o long
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string Cvc { get; set; }
        public string CardholderName { get; set; }

        // Datos del Pago
        public decimal Amount { get; set; }
        public string Currency { get; set; }  // "CRC", "USD", etc.
        public string Description { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

}
