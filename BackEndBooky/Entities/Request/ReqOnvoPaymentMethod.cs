namespace Entities.Request
{
    public class ReqOnvoPaymentMethod
    {
        public string CustomerId { get; set; }
        public string CardNumber { get; set; }
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }

        public string cvv { get; set; }
        public string CardholderName { get; set; }
       
    }
}