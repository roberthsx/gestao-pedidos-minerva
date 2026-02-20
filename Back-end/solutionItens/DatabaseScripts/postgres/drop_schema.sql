-- =============================================================================
-- Minerva.GestaoPedidos - DROP das tabelas (schema) do PostgreSQL
-- =============================================================================
-- Remove todas as tabelas do banco minerva_db, na ordem correta (FKs).
-- O banco em si NÃO é removido; use drop_database.sql para isso.
--
-- Executar conectado ao banco minerva_db:
--   psql -U admin -d minerva_db -f postgres/drop_schema.sql
-- =============================================================================

-- Ordem: tabelas que referenciam outras primeiro
DROP TABLE IF EXISTS "OrderItems";
DROP TABLE IF EXISTS "DeliveryTerms";
DROP TABLE IF EXISTS "Orders";
DROP TABLE IF EXISTS "PaymentConditions";
DROP TABLE IF EXISTS "Customers";
DROP TABLE IF EXISTS "Users";
DROP TABLE IF EXISTS "Profiles";
