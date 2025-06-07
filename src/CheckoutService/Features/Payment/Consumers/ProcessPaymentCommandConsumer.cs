using System.Threading.Tasks;
using MassTransit;
using EventContracts;
using Microsoft.Extensions.Logging;
using System; // For Guid.NewGuid()

namespace CheckoutService.Features.Payment.Consumers
{
    // This consumer simulates an external payment service
    public class ProcessPaymentCommandConsumer : IConsumer<ProcessPaymentCommand>
    {
        private readonly ILogger<ProcessPaymentCommandConsumer> _logger;

        public ProcessPaymentCommandConsumer(ILogger<ProcessPaymentCommandConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation("Simulated Payment Processor: Received ProcessPaymentCommand for OrderId: {OrderId}, Amount: {Amount}, CorrelationId: {CorrelationId}",
                command.OrderId, command.Amount, command.CorrelationId);

            // Simulate payment processing delay
            await Task.Delay(1000);

            // Simulate success/failure (e.g., based on amount or random)
            if (command.Amount > 0 && command.Amount < 10000) // Simulate success for reasonable amounts
            {
                var paymentId = Guid.NewGuid();
                _logger.LogInformation("Simulated Payment Processor: Payment successful for OrderId: {OrderId}. PaymentId: {PaymentId}", command.OrderId, paymentId);
                await context.Publish(new PaymentProcessedEvent
                {
                    CorrelationId = command.CorrelationId,
                    OrderId = command.OrderId,
                    PaymentId = paymentId,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                var reason = command.Amount <= 0 ? "Invalid payment amount." : "Payment amount too large (simulated failure).";
                _logger.LogWarning("Simulated Payment Processor: Payment failed for OrderId: {OrderId}. Reason: {Reason}", command.OrderId, reason);
                await context.Publish(new PaymentFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    OrderId = command.OrderId,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
