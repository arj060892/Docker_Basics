using System;
using System.Collections.Generic;

namespace EventContracts
{
    public record OrderConfirmedEvent
    {
        public Guid CorrelationId { get; init; }
        public Guid OrderId { get; init; }
        public Guid UserId { get; init; }
        public List<CartItemDto> Items { get; init; } // Final items
        public decimal TotalAmount { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
