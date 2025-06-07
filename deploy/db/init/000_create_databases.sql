-- deploy/db/init/000_create_databases.sql
SELECT 'CREATE DATABASE "ShoppingCartDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ShoppingCartDb')\gexec
SELECT 'CREATE DATABASE "CheckoutDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'CheckoutDb')\gexec
