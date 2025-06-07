using System;
using System.Collections.Generic;
using System.Linq;
// Ensure ValueObjects namespace is accessible if AddressData is used directly in constructor or methods
using CheckoutService.Domain.ValueObjects;

namespace CheckoutService.Domain.Aggregates
{
    public enum OrderStatus
    {
        Pending,        // Initial state when order is created from cart
        AwaitingPayment,  // Payment process initiated
        Paid,           // Payment successful
        Confirmed,      // Order confirmed, processing complete (Saga success)
        Shipped,        // Example of a further state
        Cancelled,      // Order cancelled (e.g., payment failed, stock issue)
        Failed          // Order processing failed (Saga failure)
    }

    public class Order
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        private readonly List<OrderItem> _orderItems = new List<OrderItem>();
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
        public decimal TotalAmount { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }

        // Billing Address - Could be a Value Object
        public string BillingAddressStreet { get; private set; }
        public string BillingAddressCity { get; private set; }
        public string BillingAddressZipCode { get; private set; }

        // Shipping Address - Could be a Value Object
        public string ShippingAddressStreet { get; private set; }
        public string ShippingAddressCity { get; private set; }
        public string ShippingAddressZipCode { get; private set; }


        // Private constructor for Dapper/ORM
        private Order() { }

        public Order(Guid userId, IEnumerable<OrderItemData> items, AddressData billingAddress, AddressData shippingAddress)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            Status = OrderStatus.Pending;

            if (items == null || !items.Any()) throw new ArgumentException("Order must have at least one item.");

            foreach (var itemData in items)
            {
                _orderItems.Add(new OrderItem(Id, itemData.ProductId, itemData.ProductName, itemData.UnitPrice, itemData.Quantity));
            }
            TotalAmount = _orderItems.Sum(oi => oi.TotalPrice);

            SetBillingAddress(billingAddress);
            SetShippingAddress(shippingAddress);
        }

        public void SetBillingAddress(AddressData address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            BillingAddressStreet = address.Street ?? throw new ArgumentNullException(nameof(address.Street));
            BillingAddressCity = address.City ?? throw new ArgumentNullException(nameof(address.City));
            BillingAddressZipCode = address.ZipCode ?? throw new ArgumentNullException(nameof(address.ZipCode));
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void SetShippingAddress(AddressData address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            ShippingAddressStreet = address.Street ?? throw new ArgumentNullException(nameof(address.Street));
            ShippingAddressCity = address.City ?? throw new ArgumentNullException(nameof(address.City));
            ShippingAddressZipCode = address.ZipCode ?? throw new ArgumentNullException(nameof(address.ZipCode));
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void SetStatusToAwaitingPayment()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException("Order status can only be set to AwaitingPayment from Pending.");
            Status = OrderStatus.AwaitingPayment;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void SetStatusToPaid()
        {
            if (Status != OrderStatus.AwaitingPayment)
                throw new InvalidOperationException("Order status can only be set to Paid from AwaitingPayment.");
            Status = OrderStatus.Paid;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void ConfirmOrder()
        {
            if (Status != OrderStatus.Paid)
                throw new InvalidOperationException("Order cannot be confirmed if not in a valid preceding state (e.g., Paid).");
            Status = OrderStatus.Confirmed;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void CancelOrder()
        {
            Status = OrderStatus.Cancelled;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void FailOrder()
        {
            Status = OrderStatus.Failed;
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
}
