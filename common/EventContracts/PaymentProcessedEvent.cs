using System;

namespace EventContracts
{
    public record PaymentProcessedEvent
    {
        public Guid CorrelationId { get; init; } // Saga CorrelationId
        public Guid OrderId { get; init; }
        public Guid PaymentId { get; init; } // ID from payment gateway
        public DateTime Timestamp { get; init; }
    }
}
