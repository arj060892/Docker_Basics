using MediatR;
using MassTransit;
using ShoppingCartService.Infrastructure.Data;
using ShoppingCartService.Domain.Contracts;
using ShoppingCartService.Infrastructure.Persistence;
using ShoppingCartService.Features.Cart.Commands.AddItemToCart;
using ShoppingCartService.Features.Cart.Queries.GetCartById;
using Microsoft.AspNetCore.Mvc;
using EventContracts;
using AspNetCoreRateLimit; // Added
using System.Linq; // Required for .Any() and .Select()

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Needed for AspNetCoreRateLimit
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddHttpContextAccessor(); // Required by RateLimitConfiguration

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartReadRepository, CartRepository>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("user");
            h.Password("password");
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseIpRateLimiting(); // Added - Should be early

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// API Endpoints (existing)
app.MapPost("/api/carts/items", async (IMediator mediator, [FromBody] AddItemToCartCommand command) =>
{
    if (command.ProductId == Guid.Empty || command.Quantity <= 0 || string.IsNullOrEmpty(command.ProductName) || command.UnitPrice < 0)
    {
        return Results.BadRequest("Invalid item data.");
    }
    var cartId = await mediator.Send(command);
    return Results.Created($"/api/carts/{cartId}", new { CartId = cartId });
})
.WithName("AddItemToCart")
.Produces<object>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/api/carts/{cartId}", async (IMediator mediator, Guid cartId) =>
{
    if (cartId == Guid.Empty) return Results.BadRequest("Invalid Cart ID.");
    var query = new GetCartByIdQuery { CartId = cartId };
    var cart = await mediator.Send(query);
    if (cart == null || cart.Id == Guid.Empty) return Results.NotFound();
    return Results.Ok(cart);
})
.WithName("GetCartById")
.Produces<CartDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/carts/{cartId}/checkout", async (Guid cartId, IMediator cartQueryMediator, IPublishEndpoint publishEndpoint) =>
{
    if (cartId == Guid.Empty) return Results.BadRequest("Invalid Cart ID.");
    var query = new GetCartByIdQuery { CartId = cartId };
    var cartDto = await cartQueryMediator.Send(query);
    if (cartDto == null || cartDto.Id == Guid.Empty) return Results.NotFound("Cart not found.");
    if (!cartDto.Items.Any()) return Results.BadRequest("Cannot checkout an empty cart.");
    var eventMessage = new CartCheckedOutEvent
    {
        CorrelationId = Guid.NewGuid(), CartId = cartDto.Id, UserId = cartDto.UserId,
        Items = cartDto.Items.Select(item => new EventContracts.CartItemDto
        { ProductId = item.ProductId, ProductName = item.ProductName, UnitPrice = item.UnitPrice, Quantity = item.Quantity }).ToList(),
        TotalPrice = cartDto.TotalPrice, Timestamp = DateTime.UtcNow
    };
    await publishEndpoint.Publish(eventMessage);
    return Results.Accepted(value: new { eventMessage.CorrelationId });
})
.WithName("CheckoutCart")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () => "Hello from ShoppingCartService!");
app.Run();
