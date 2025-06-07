using System;

namespace CheckoutService.Domain.Aggregates
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public Guid OrderId { get; private set; } // Foreign key to Order
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } // Denormalized from cart
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalPrice => UnitPrice * Quantity;

        // Private constructor for Dapper/ORM
        private OrderItem() { }

        // Internal constructor to ensure it's created via Order aggregate
        internal OrderItem(Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

            Id = Guid.NewGuid();
            OrderId = orderId;
            ProductId = productId;
            ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
    }
}
