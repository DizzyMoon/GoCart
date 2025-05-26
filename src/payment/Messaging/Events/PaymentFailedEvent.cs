using System;

namespace payment.Messaging.Events
{
    public class PaymentFailedEvent
    {
        public string? PaymentIntentId { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        // This reference hold Stripe token if PaymentIntentId isn't available
        public string PaymentAttemptReference { get; set; } 
    }
}