using System;
using System.Linq;
using MassTransit;
using EventContracts;
using CheckoutService.Domain.Sagas;
using CheckoutService.Features.Checkout.Commands.InitiateCheckout; // To create the order
using CheckoutService.Domain.ValueObjects; // For AddressData in InitiateCheckoutCommand
using MediatR; // To send commands
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // For List in OrderItemData

namespace CheckoutService.Sagas
{
    public class OrderSaga : MassTransitStateMachine<OrderSagaInstance>
    {
        private readonly ILogger<OrderSaga> _logger;
        // MediatR for sending commands like InitiateCheckoutCommand
        // MassTransit recommends using IRequestClient<TRequest> for MediatR within sagas if possible,
        // or directly injecting IMediator for fire-and-forget style.
        // For simplicity, we might make InitiateCheckoutCommand idempotent or the saga handles retries.
        // Let's assume IMediator is available via constructor injection (configured in Program.cs for sagas)

        // Define States
        public State Submitted { get; private set; }        // Initial state after CartCheckedOutEvent
        public State AwaitingPayment { get; private set; }  // Waiting for payment processing
        public State OrderCreated { get; private set; }     // Order persisted in DB
        public State PaymentProcessed { get; private set; } // Payment was successful
        public State OrderConfirmed { get; private set; }   // Order successfully processed
        public State OrderFailed { get; private set; }      // Order processing failed

        // Define Events that trigger state transitions
        public Event<CartCheckedOutEvent> CartCheckoutSubmittedEvent { get; private set; }
        public Event<PaymentProcessedEvent> PaymentSucceededEvent { get; private set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; private set; }
        // Internal event to confirm order creation in DB
        public Event<OrderCreationConfirmed> OrderCreationConfirmedEvent { get; private set; }
        public Event<OrderCreationFailed> OrderCreationFailedEvent { get; private set; }


        public OrderSaga(ILogger<OrderSaga> logger) // IMediator mediator will be passed by MT if registered
        {
            _logger = logger;
            // IMediator is not directly available here in the constructor of the state machine definition.
            // It's typically used within the .ThenAsync() blocks by accessing it from the ConsumeContext
            // or by using a ServiceProvider. For now, we'll focus on the state machine logic.
            // Activities will handle sending commands.

            InstanceState(x => x.CurrentState); // Map the CurrentState property of OrderSagaInstance

            // Define Event Correlation: How incoming events are matched to saga instances
            // CartCheckedOutEvent initiates a new saga instance if one doesn't exist for its CorrelationId.
            Event(() => CartCheckoutSubmittedEvent, x => x.CorrelateById(context => context.Message.CorrelationId)
                .SelectId(context => context.Message.CorrelationId)); // Use CorrelationId from event

            Event(() => OrderCreationConfirmedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderCreationFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PaymentSucceededEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));


            // Define Saga Flow (Behavior)
            Initially(
                When(CartCheckoutSubmittedEvent)
                    .Then(context => {
                        _logger.LogInformation("Saga Initialized with CartCheckoutSubmittedEvent. CorrelationId: {CorrelationId}", context.Saga.CorrelationId);
                        context.Saga.UserId = context.Message.UserId;
                        context.Saga.TotalAmount = context.Message.TotalPrice;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        // Store items if needed, or assume InitiateCheckoutCommand handles it
                    })
                    .Activity(x => x.OfType<CreateOrderActivity>()) // Use an activity to send command
                    .TransitionTo(Submitted)
            );

            During(Submitted,
                When(OrderCreationConfirmedEvent)
                    .Then(context => {
                        _logger.LogInformation("Order Creation Confirmed. OrderId: {OrderId}, CorrelationId: {CorrelationId}", context.Message.OrderId, context.Saga.CorrelationId);
                        context.Saga.OrderId = context.Message.OrderId; // Store OrderId from the command response
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new ProcessPaymentCommand // Publish command to process payment
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrderId = context.Saga.OrderId.Value,
                        UserId = context.Saga.UserId,
                        Amount = context.Saga.TotalAmount
                    })
                    .TransitionTo(AwaitingPayment),
                When(OrderCreationFailedEvent)
                    .Then(context => {
                        _logger.LogError("Order Creation Failed. Reason: {Reason}, CorrelationId: {CorrelationId}", context.Message.Reason, context.Saga.CorrelationId);
                        context.Saga.FailureReason = context.Message.Reason;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new OrderFailedEvent { // Publish final failure event
                        CorrelationId = context.Saga.CorrelationId,
                        OrderId = context.Saga.OrderId ?? Guid.Empty,
                        UserId = context.Saga.UserId,
                        Reason = context.Saga.FailureReason,
                        Timestamp = DateTime.UtcNow
                    })
                    .TransitionTo(OrderFailed)
            );

            During(AwaitingPayment,
                When(PaymentSucceededEvent)
                    .Then(context => {
                        _logger.LogInformation("Payment Succeeded. OrderId: {OrderId}, PaymentId: {PaymentId}, CorrelationId: {CorrelationId}", context.Message.OrderId, context.Message.PaymentId, context.Saga.CorrelationId);
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                        // Here you would typically confirm the order, update inventory, etc.
                        // For simplicity, we'll transition to OrderConfirmed directly.
                    })
                    .Publish(context => new OrderConfirmedEvent { // Publish final success event
                        CorrelationId = context.Saga.CorrelationId,
                        OrderId = context.Saga.OrderId.Value,
                        UserId = context.Saga.UserId,
                        TotalAmount = context.Saga.TotalAmount,
                        Items = new List<EventContracts.CartItemDto>(), // Items = ... if needed and stored in saga instance
                        Timestamp = DateTime.UtcNow
                    })
                    .TransitionTo(OrderConfirmed)
                    .Finalize(), // End the saga successfully

                When(PaymentFailedEvent)
                    .Then(context => {
                        _logger.LogError("Payment Failed. OrderId: {OrderId}, Reason: {Reason}, CorrelationId: {CorrelationId}", context.Message.OrderId, context.Message.Reason, context.Saga.CorrelationId);
                        context.Saga.FailureReason = context.Message.Reason;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                        // Optionally, publish a command to cancel the order in DB if it was created
                    })
                    .Publish(context => new OrderFailedEvent { // Publish final failure event
                        CorrelationId = context.Saga.CorrelationId,
                        OrderId = context.Saga.OrderId.Value,
                        UserId = context.Saga.UserId,
                        Reason = context.Saga.FailureReason,
                        Timestamp = DateTime.UtcNow
                    })
                    .TransitionTo(OrderFailed)
                    .Finalize() // End the saga due to failure
            );

            // Define what happens when a saga is finalized (completed or failed)
            SetCompletedWhenFinalized();
        }
    }

    // Define internal events for saga steps if needed (e.g. confirming DB operations)
    public record OrderCreationConfirmed { public Guid CorrelationId { get; init; } public Guid OrderId { get; init; } }
    public record OrderCreationFailed { public Guid CorrelationId { get; init; } public string Reason { get; init; } }

    // Saga Activity for creating order
    public class CreateOrderActivity : IStateMachineActivity<OrderSagaInstance, CartCheckedOutEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateOrderActivity> _logger;

        public CreateOrderActivity(IMediator mediator, ILogger<CreateOrderActivity> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public void Probe(ProbeContext context) => context.CreateScope("create-order");
        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(BehaviorContext<OrderSagaInstance, CartCheckedOutEvent> context, IBehavior<OrderSagaInstance, CartCheckedOutEvent> next)
        {
            _logger.LogInformation("CreateOrderActivity: Creating order for CorrelationId: {CorrelationId}", context.Saga.CorrelationId);
            var command = new InitiateCheckoutCommand
            {
                CorrelationId = context.Saga.CorrelationId,
                UserId = context.Message.UserId,
                Items = context.Message.Items.Select(item => new CheckoutService.Domain.Aggregates.OrderItemData
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                }).ToList(),
                BillingAddress = new AddressData("Saga St", "Saga City", "S0000"), // Placeholder
                ShippingAddress = new AddressData("Saga Rd", "Saga Town", "S1111") // Placeholder
            };

            try
            {
                var orderId = await _mediator.Send(command);
                _logger.LogInformation("CreateOrderActivity: Order created successfully. OrderId: {OrderId}, CorrelationId: {CorrelationId}", orderId, context.Saga.CorrelationId);
                // Publish internal event for saga to react to
                await context.Publish(new OrderCreationConfirmed { CorrelationId = context.Saga.CorrelationId, OrderId = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOrderActivity: Failed to create order for CorrelationId: {CorrelationId}", context.Saga.CorrelationId);
                await context.Publish(new OrderCreationFailed { CorrelationId = context.Saga.CorrelationId, Reason = ex.Message });
            }
            await next.Execute(context).ConfigureAwait(false);
        }
    }
}
