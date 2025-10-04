using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvoPaymentLinkResponse : ResBase
    {
        public bool resultado { get; set; }
        public string mensaje { get; set; }
        public string paymentLinkId { get; set; }
        public string paymentUrl { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string status { get; set; }
        public DateTime? expiresAt { get; set; }
        public List<Error> error { get; set; }
    }
}
