using System;

namespace EventContracts
{
    public record ProcessPaymentCommand
    {
        public Guid CorrelationId { get; init; } // Saga CorrelationId
        public Guid OrderId { get; init; }
        public Guid UserId { get; init; }
        public decimal Amount { get; init; }
    }
}
