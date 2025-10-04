using System;

namespace Entities.Response
{
    public class ResOnvoPaymentLink : ResBase
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}