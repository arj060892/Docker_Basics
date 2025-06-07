using System;

namespace ShoppingCartService.Domain.Aggregates
{
    public class CartItem
    {
        public Guid Id { get; private set; }
        public Guid CartId { get; private set; } // Foreign key to Cart
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } // Denormalized for easier cart display
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalPrice => UnitPrice * Quantity;

        // Private constructor for Dapper/ORM
        private CartItem() { }

        // Internal constructor to ensure it's created via Cart aggregate
        internal CartItem(Guid cartId, Guid productId, string productName, decimal unitPrice, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

            Id = Guid.NewGuid();
            CartId = cartId;
            ProductId = productId;
            ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        internal void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(newQuantity), "New quantity must be positive. To remove an item, use the Cart's RemoveItem method or set quantity to zero in UpdateItemQuantity.");
            Quantity = newQuantity;
        }
    }
}
