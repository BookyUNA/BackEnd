using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class OnvoWebhookEvent
    {
        public string EventType { get; set; }
        public string PaymentId { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
