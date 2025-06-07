using System;
using System.Threading.Tasks;
using CheckoutService.Domain.Aggregates;
using CheckoutService.Features.Checkout.Queries.GetOrderStatus; // For OrderStatusDto

namespace CheckoutService.Domain.Contracts
{
    public interface IOrderRepository
    {
        Task<Order> GetByIdAsync(Guid orderId);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
    }

    public interface IOrderReadRepository
    {
        Task<OrderStatusDto> GetOrderStatusDtoByIdAsync(Guid orderId);
    }
}
