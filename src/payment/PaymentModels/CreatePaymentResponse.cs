namespace payment.PaymentModels
{
    public class CreatePaymentResponse
    {
        public string ClientSecret { get; set; }
        public string PaymentIntentId { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
    }    
}

