using System;
using MediatR;

namespace ShoppingCartService.Features.Cart.Commands.AddItemToCart
{
    public class AddItemToCartCommand : IRequest<Guid> // Assuming it returns CartId or some identifier
    {
        public Guid CartId { get; set; } // Can be existing cart or new if 0/null
        public Guid UserId { get; set; } // If creating a new cart
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
