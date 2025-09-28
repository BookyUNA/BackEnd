using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvo
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

    }
}
