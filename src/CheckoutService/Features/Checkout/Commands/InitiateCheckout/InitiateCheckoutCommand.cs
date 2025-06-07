using System;
using System.Collections.Generic;
using MediatR;
using CheckoutService.Domain.Aggregates; // For OrderItemData
using CheckoutService.Domain.ValueObjects; // For AddressData

namespace CheckoutService.Features.Checkout.Commands.InitiateCheckout
{
    public class InitiateCheckoutCommand : IRequest<Guid> // Returns OrderId
    {
        public Guid UserId { get; set; }
        public List<OrderItemData> Items { get; set; }
        public AddressData BillingAddress { get; set; }
        public AddressData ShippingAddress { get; set; }
        public Guid CorrelationId { get; set; } // For Saga correlation
    }
}
