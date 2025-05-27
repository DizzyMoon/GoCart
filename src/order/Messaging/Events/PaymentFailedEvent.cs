using System;

namespace order.Messaging.Events
{
    public class PaymentFailedEvent 
    {
        public string? PaymentIntentId { get; set; } // Nullable if PI creation failed early
        public string Reason { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string PaymentAttemptReference { get; set; } // e.g., Stripe token if PI failed
    }
}