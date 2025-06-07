using System;

namespace EventContracts
{
    public record PaymentFailedEvent
    {
        public Guid CorrelationId { get; init; } // Saga CorrelationId
        public Guid OrderId { get; init; }
        public string Reason { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
