using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvoPaymentMethodResponse
    {
        public bool resultado { get; set; }
        public string mensaje { get; set; }
        public string paymentMethodId { get; set; }
        public string last4 { get; set; }
        public string brand { get; set; }
        public string expMonth { get; set; }
        public string expYear { get; set; }
        public List<Error> error { get; set; }
    }
}
