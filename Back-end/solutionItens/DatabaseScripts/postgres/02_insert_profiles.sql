-- =============================================================================
-- Minerva.GestaoPedidos - Inserts dos perfis (Admin, Gestão, Analista)
-- =============================================================================
-- Executar após 01_schema.sql e antes de 02_insert_users.sql.
-- IDs fixos 1, 2, 3 para referência nos usuários.
-- =============================================================================

INSERT INTO "Profiles" ("Id", "Code", "Name")
VALUES
    (1, 'ADMIN', 'Admin'),
    (2, 'GESTAO', 'Gestão'),
    (3, 'ANALISTA', 'Analista')
ON CONFLICT ("Code") DO NOTHING;

-- Ajusta a sequence para o próximo Id ser 4 (evita conflito em novos inserts sem Id).
SELECT setval(pg_get_serial_sequence('"Profiles"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Profiles"), 1));
