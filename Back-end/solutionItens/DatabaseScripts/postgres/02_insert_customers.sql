-- =============================================================================
-- Minerva.GestaoPedidos - Seed para a tabela Customers (carga de clientes)
-- =============================================================================
-- Executar APÓS 01_schema.sql (tabela Customers deve existir).
-- =============================================================================

INSERT INTO "Customers" ("Name", "Email", "CreatedAtUtc") VALUES
('YUM! Brands Brasil (KFC/Pizza Hut)', 'procurement@yum.com', CURRENT_TIMESTAMP),
('Carrefour Brasil', 'comercial@carrefour.com.br', CURRENT_TIMESTAMP),
('Grupo Pão de Açúcar', 'compras@gpa.com.br', CURRENT_TIMESTAMP),
('Atacadão S.A.', 'abastecimento@atacadao.com.br', CURRENT_TIMESTAMP),
('Shopper.com.br', 'parcerias@shopper.com.br', CURRENT_TIMESTAMP),
('Churrascaria Fogo de Chão', 'suprimentos@fogodechao.com.br', CURRENT_TIMESTAMP),
('Mercado Livre (Supermercado)', 'b2b@mercadolivre.com', CURRENT_TIMESTAMP),
('Importadora Beijing Beef', 'global@beijingbeef.cn', CURRENT_TIMESTAMP);
