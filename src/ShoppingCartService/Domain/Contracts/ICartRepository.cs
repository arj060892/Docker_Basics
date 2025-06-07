using System;
using System.Threading.Tasks;
using ShoppingCartService.Domain.Aggregates;
using ShoppingCartService.Features.Cart.Queries.GetCartById; // Added for CartDto

namespace ShoppingCartService.Domain.Contracts
{
    public interface ICartRepository
    {
        Task<Cart> GetByIdAsync(Guid cartId);
        Task AddAsync(Cart cart);
        Task UpdateAsync(Cart cart);
        Task<Cart> GetByUserIdAsync(Guid userId);
    }

    public interface ICartReadRepository
    {
        Task<CartDto> GetCartDtoByIdAsync(Guid cartId);
    }
}
