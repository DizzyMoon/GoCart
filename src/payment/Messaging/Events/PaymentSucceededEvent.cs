using System;

namespace payment.Messaging.Events
{
    public class PaymentSucceededEvent
    {
        public string PaymentIntentId { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}