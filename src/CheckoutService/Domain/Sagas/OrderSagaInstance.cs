using System;
using MassTransit;

namespace CheckoutService.Domain.Sagas
{
    public class OrderSagaInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } // PK
        public string CurrentState { get; set; }
        public int Version { get; set; } // For optimistic concurrency with Dapper

        // Custom properties for the Order Saga
        public Guid? OrderId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        // The OrderPayload JSONB column from SQL schema can be used to store List<OrderItemData> if needed,
        // or add specific fields here if preferred and map them.
        // For Dapper persistence, properties must match table columns or be ignored.
        // Let's assume OrderPayload is not directly mapped for now to keep it simple.
    }
}
