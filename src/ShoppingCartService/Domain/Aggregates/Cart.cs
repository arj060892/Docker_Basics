using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingCartService.Domain.Aggregates
{
    public class Cart
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        private readonly List<CartItem> _items = new List<CartItem>();
        public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        // Private constructor for Dapper/ORM
        private Cart() { }

        public Cart(Guid userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            var existingItem = _items.FirstOrDefault(item => item.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            }
            else
            {
                _items.Add(new CartItem(Id, productId, productName, unitPrice, quantity));
            }
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void UpdateItemQuantity(Guid productId, int newQuantity)
        {
            if (newQuantity < 0) throw new ArgumentOutOfRangeException(nameof(newQuantity), "Quantity cannot be negative.");

            var itemToUpdate = _items.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate == null) throw new InvalidOperationException("Item not found in cart.");

            if (newQuantity == 0)
            {
                RemoveItem(productId);
            }
            else
            {
                itemToUpdate.UpdateQuantity(newQuantity);
            }
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void RemoveItem(Guid productId)
        {
            var itemToRemove = _items.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                _items.Remove(itemToRemove);
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        public decimal TotalPrice => _items.Sum(item => item.TotalPrice);
    }
}
