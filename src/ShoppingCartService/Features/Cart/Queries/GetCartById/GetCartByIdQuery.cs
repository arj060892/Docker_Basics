using System;
using MediatR;
using ShoppingCartService.Domain.Aggregates; // Or a DTO
using System.Collections.Generic; // Required for List

namespace ShoppingCartService.Features.Cart.Queries.GetCartById
{
    // Define a DTO for the cart response
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class GetCartByIdQuery : IRequest<CartDto>
    {
        public Guid CartId { get; set; }
    }
}
