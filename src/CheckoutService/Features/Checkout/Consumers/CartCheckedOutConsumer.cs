using System.Threading.Tasks;
using MassTransit;
using EventContracts;
using MediatR; // To trigger InitiateCheckoutCommand
using CheckoutService.Features.Checkout.Commands.InitiateCheckout;
using Microsoft.Extensions.Logging;
using CheckoutService.Domain.ValueObjects; // For AddressData
using System.Linq; // For Select
using System.Collections.Generic; // For List

namespace CheckoutService.Features.Checkout.Consumers
{
    public class CartCheckedOutConsumer : IConsumer<CartCheckedOutEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CartCheckedOutConsumer> _logger;

        public CartCheckedOutConsumer(IMediator mediator, ILogger<CartCheckedOutConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CartCheckedOutEvent> context)
        {
            _logger.LogInformation("Received CartCheckedOutEvent for CartId: {CartId}, CorrelationId: {CorrelationId}",
                context.Message.CartId, context.Message.CorrelationId);

            // Here, we would map CartCheckedOutEvent to InitiateCheckoutCommand
            // The InitiateCheckoutCommand requires address information which is not in CartCheckedOutEvent yet.
            // For now, we'll create dummy addresses. This highlights a design consideration:
            // - Either CartCheckedOutEvent includes addresses (if collected by ShoppingCart)
            // - Or CheckoutService has a step to collect addresses after this event.
            // The current InitiateCheckoutCommand expects addresses.
            // Let's assume for now addresses are collected by another mechanism or are default.

            var command = new InitiateCheckoutCommand
            {
                CorrelationId = context.Message.CorrelationId, // CRITICAL for Saga
                UserId = context.Message.UserId,
                Items = context.Message.Items.Select(item => new Domain.Aggregates.OrderItemData
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                }).ToList(),
                // Placeholder addresses - this needs to be properly handled in a real scenario
                BillingAddress = new AddressData("123 Billing St", "Billing City", "B1234"),
                ShippingAddress = new AddressData("456 Shipping Rd", "Shipping City", "S5678")
            };

            try
            {
                var orderId = await _mediator.Send(command);
                _logger.LogInformation("InitiateCheckoutCommand sent successfully for CorrelationId: {CorrelationId}. OrderId: {OrderId}",
                    command.CorrelationId, orderId);
                // The command handler might initiate the Saga, or the Saga itself might be started by this event.
                // If InitiateCheckoutCommand starts the Saga, this is fine.
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing CartCheckedOutEvent and sending InitiateCheckoutCommand for CorrelationId: {CorrelationId}", command.CorrelationId);
                // Consider publishing an error event or implementing retry mechanisms via MassTransit
                throw; // Re-throw to allow MassTransit to handle retry/error logic
            }
        }
    }
}
