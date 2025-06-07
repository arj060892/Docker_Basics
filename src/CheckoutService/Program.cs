using MediatR;
using MassTransit;
using CheckoutService.Infrastructure.Data;
using CheckoutService.Domain.Contracts;
using CheckoutService.Infrastructure.Persistence;
using CheckoutService.Features.Checkout.Commands.InitiateCheckout;
using CheckoutService.Features.Checkout.Queries.GetOrderStatus;
using CheckoutService.Sagas;
using CheckoutService.Domain.Sagas;
using CheckoutService.Features.Payment.Consumers;
using Microsoft.AspNetCore.Mvc;
using EventContracts;
using MassTransit.DapperIntegration;
using System.Data;
using AspNetCoreRateLimit; // Added
using System.Linq; // Required for .Any()

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderReadRepository, OrderRepository>();

var connectionString = builder.Configuration.GetConnectionString("CheckoutDb");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProcessPaymentCommandConsumer>();
    x.AddSagaStateMachine<OrderSaga, OrderSagaInstance>()
        .DapperRepository(connectionString, cfg => {
            cfg.Dialect = DapperSqlDialect.PostgreSQL;
        });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h => { h.Username("user"); h.Password("password"); });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddScoped<CreateOrderActivity>();

var app = builder.Build();

app.UseIpRateLimiting(); // Added - Should be early

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// API Endpoints (existing)
app.MapPost("/api/checkout", async (IMediator mediator, [FromBody] InitiateCheckoutCommand command) =>
{
    if (command.UserId == Guid.Empty || command.Items == null || !command.Items.Any())
        return Results.BadRequest("Invalid checkout data.");
    if (command.BillingAddress == null || command.ShippingAddress == null)
        return Results.BadRequest("Billing and Shipping addresses are required.");
    if(command.CorrelationId == Guid.Empty) command.CorrelationId = Guid.NewGuid();
    var orderId = await mediator.Send(command);
    return Results.Accepted($"/api/orders/{orderId}/status", new { OrderId = orderId, CorrelationId = command.CorrelationId });
})
.WithName("InitiateCheckout")
.Produces<object>(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/api/orders/{orderId}/status", async (IMediator mediator, Guid orderId) =>
{
    if (orderId == Guid.Empty) return Results.BadRequest("Invalid Order ID.");
    var query = new GetOrderStatusQuery { OrderId = orderId };
    var status = await mediator.Send(query);
    if (status == null || status.OrderId == Guid.Empty) return Results.NotFound();
    return Results.Ok(status);
})
.WithName("GetOrderStatus")
.Produces<OrderStatusDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () => "Hello from CheckoutService!");
app.Run();
