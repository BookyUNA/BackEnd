using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqVerifyPayment
    {
        public string PaymentId { get; set; }
        public string ReferenceId { get; set; }
    }
}
