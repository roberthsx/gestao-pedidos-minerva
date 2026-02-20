-- =============================================================================
-- Minerva.GestaoPedidos - DROP do banco de dados PostgreSQL
-- =============================================================================
-- Remove o banco minerva_db. Nenhuma conexão pode estar usando o banco.
-- Executar conectado a outro banco (ex.: postgres) como superuser:
--
--   psql -U postgres -d postgres -f postgres/drop_database.sql
--
-- Ou via linha de comando:
--   psql -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'minerva_db' AND pid <> pg_backend_pid();"
--   psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS minerva_db;"
-- =============================================================================

-- Encerra conexões ativas no banco (evita erro "database is being accessed by other users")
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'minerva_db'
  AND pid <> pg_backend_pid();

-- Remove o banco
DROP DATABASE IF EXISTS minerva_db;
