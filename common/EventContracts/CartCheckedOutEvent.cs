using System;
using System.Collections.Generic;

namespace EventContracts
{
    public record CartItemDto // Re-define or ensure consistency if also in ShoppingCartService features
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; }
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
    }

    public record CartCheckedOutEvent
    {
        public Guid CorrelationId { get; init; } // Important for Saga correlation
        public Guid CartId { get; init; }
        public Guid UserId { get; init; }
        public List<CartItemDto> Items { get; init; }
        public decimal TotalPrice { get; init; }
        public DateTime Timestamp { get; init; }

        // Include billing/shipping info if it's captured at cart checkout stage
        // For now, assuming it's collected by CheckoutService after this event
    }
}
