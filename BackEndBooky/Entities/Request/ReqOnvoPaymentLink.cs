using System;
using System.Collections.Generic;

namespace Entities.Request
{
    public class ReqOnvoPaymentLink
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "CRC";
        public string Description { get; set; }
        public string CustomerId { get; set; }
      
        public DateTime? ExpiresAt { get; set; }
       
    }
}
