using System;
using MediatR;
using CheckoutService.Domain.Aggregates; // For OrderStatus enum

namespace CheckoutService.Features.Checkout.Queries.GetOrderStatus
{
    public class OrderStatusDto
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class GetOrderStatusQuery : IRequest<OrderStatusDto>
    {
        public Guid OrderId { get; set; }
    }
}
