using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqOnvoPayment
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        
    }

  
}
