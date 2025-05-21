namespace payment.PaymentModels
{
    public class CreatePaymentRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; }
        
        public string Token { get; set; }
        public string CardholderName { get; set; }
    }
}

