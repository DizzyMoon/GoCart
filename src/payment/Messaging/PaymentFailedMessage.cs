namespace payment.Messaging
{
    public class PaymentFailedMessage
    {
        public string PaymentIntentId { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public int RetryCount { get; set; } = 0;
    }    
}

