-- =============================================================================
-- Minerva.GestaoPedidos - Seed para a tabela PaymentConditions (meios de pagamento B2B)
-- =============================================================================
-- Condições comuns ao setor (varejo/carnes B2B). Executar APÓS 01_schema.sql.
-- =============================================================================

-- Boleto Bancário (prazos conforme análise de crédito)
INSERT INTO "PaymentConditions" ("Description", "NumberOfInstallments", "CreatedAtUtc") VALUES
('Boleto à vista', 1, CURRENT_TIMESTAMP),
('Boleto 7 dias', 1, CURRENT_TIMESTAMP),
('Boleto 14 dias', 1, CURRENT_TIMESTAMP),
('Boleto 21 dias', 1, CURRENT_TIMESTAMP),
('Boleto 30 dias', 1, CURRENT_TIMESTAMP);

-- Cartão de Crédito (compras menores ou emergenciais)
INSERT INTO "PaymentConditions" ("Description", "NumberOfInstallments", "CreatedAtUtc") VALUES
('Cartão de Crédito 1x', 1, CURRENT_TIMESTAMP),
('Cartão de Crédito 2x', 2, CURRENT_TIMESTAMP),
('Cartão de Crédito 3x', 3, CURRENT_TIMESTAMP),
('Cartão de Crédito 4x', 4, CURRENT_TIMESTAMP),
('Cartão de Crédito 5x', 5, CURRENT_TIMESTAMP),
('Cartão de Crédito 6x', 6, CURRENT_TIMESTAMP);

-- Transferência Bancária (TED/PIX) - à vista ou antecipado
INSERT INTO "PaymentConditions" ("Description", "NumberOfInstallments", "CreatedAtUtc") VALUES
('Transferência Bancária (TED/PIX) à vista', 1, CURRENT_TIMESTAMP);

-- Cartão BNDES (financiamento para empresas elegíveis)
INSERT INTO "PaymentConditions" ("Description", "NumberOfInstallments", "CreatedAtUtc") VALUES
('Cartão BNDES', 1, CURRENT_TIMESTAMP);

-- Crédito Documentário (grandes clientes globais e exportação)
INSERT INTO "PaymentConditions" ("Description", "NumberOfInstallments", "CreatedAtUtc") VALUES
('Crédito Documentário (Carta de Crédito)', 1, CURRENT_TIMESTAMP);
