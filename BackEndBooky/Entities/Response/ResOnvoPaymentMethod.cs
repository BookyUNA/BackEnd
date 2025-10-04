using System;

namespace Entities.Response
{
    public class ResOnvoPaymentMethod : ResBase
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string Last4 { get; set; }
        public string Brand { get; set; }
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}