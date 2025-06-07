using System;

namespace EventContracts
{
    public record OrderFailedEvent
    {
        public Guid CorrelationId { get; init; }
        public Guid OrderId { get; init; } // May not exist if failure is very early
        public Guid UserId { get; init; }
        public string Reason { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
