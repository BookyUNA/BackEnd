using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
   public class OnvoPaymentLinkDetalle
    {
        public int Id { get; set; }
        public string PaymentLinkId { get; set; }
        public string ReferenceId { get; set; }
        public string PaymentUrl { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string StatusDescripcion { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }
}
