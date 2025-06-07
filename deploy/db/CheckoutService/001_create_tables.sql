-- Script to create tables for CheckoutService

-- Ensure the database exists
-- CREATE DATABASE CheckoutDb;
-- \c CheckoutDb;

-- Orders Table
CREATE TABLE IF NOT EXISTS Orders (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status VARCHAR(50) NOT NULL, -- e.g., Pending, Paid, Confirmed, Cancelled, Failed
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUpdatedAt TIMESTAMPTZ,
    BillingAddressStreet VARCHAR(255),
    BillingAddressCity VARCHAR(100),
    BillingAddressZipCode VARCHAR(20),
    ShippingAddressStreet VARCHAR(255),
    ShippingAddressCity VARCHAR(100),
    ShippingAddressZipCode VARCHAR(20)
);

-- OrderItems Table
CREATE TABLE IF NOT EXISTS OrderItems (
    Id UUID PRIMARY KEY,
    OrderId UUID NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL,
    ProductName VARCHAR(255) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    Quantity INT NOT NULL
);

-- Sagas Table (for MassTransit Saga persistence - basic structure)
-- MassTransit typically uses specific tables like 'OrderSagaInstance' for a saga named 'OrderSaga'
-- For now, a generic Sagas table that might be used by a custom saga persister or as a placeholder.
-- If using MT's EF Core persister, it creates its own schema. For Dapper/custom, we define it.
-- Let's assume a simple state persistence for now. MT's Dapper integration might need specific columns.
-- A common requirement is CorrelationId and a serialized state or specific state properties.
-- MassTransit.Components.Saga.DapperIntegration for example uses a table per saga type.
-- We'll create a generic one, and it can be adapted when Saga is implemented.
-- For MT's default (InMemoryOutboxSagaRepository) no DB table is needed for saga instance itself unless using a persistent saga repository.
-- The plan specifies "Use a table Sagas (Id, State, Timestamp, ...)"
-- Let's call it OrderSagaInstances to be more specific if our saga is OrderSaga
CREATE TABLE IF NOT EXISTS OrderSagaInstances (
    CorrelationId UUID PRIMARY KEY,
    CurrentState VARCHAR(100) NOT NULL, -- Current state of the saga
    Version INT NOT NULL DEFAULT 0,     -- For optimistic concurrency
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUpdatedAt TIMESTAMPTZ,
    -- Add other saga-specific properties as needed, potentially as a JSONB column
    OrderPayload JSONB -- Example: to store initial order data or related info
);


-- Indexes for performance
CREATE INDEX IF NOT EXISTS IDX_Orders_UserId ON Orders(UserId);
CREATE INDEX IF NOT EXISTS IDX_OrderItems_OrderId ON OrderItems(OrderId);
