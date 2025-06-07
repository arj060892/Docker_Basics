-- Script to create tables for ShoppingCartService

-- Ensure the database exists (or create it if running this manually or via a custom entrypoint)
-- CREATE DATABASE ShoppingCartDb; -- This might be handled by docker-compose or another script
-- \c ShoppingCartDb; -- Connect to the database

-- Carts Table
CREATE TABLE IF NOT EXISTS Carts (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- CartItems Table
CREATE TABLE IF NOT EXISTS CartItems (
    Id UUID PRIMARY KEY,
    CartId UUID NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL,
    ProductName VARCHAR(255) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    Quantity INT NOT NULL,
    CONSTRAINT UQ_CartItem_Cart_Product UNIQUE (CartId, ProductId) -- Ensure a product appears only once per cart
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS IDX_Carts_UserId ON Carts(UserId);
CREATE INDEX IF NOT EXISTS IDX_CartItems_CartId ON CartItems(CartId);
