-- =============================================================================
-- Minerva.GestaoPedidos - Inicialização do banco PostgreSQL (Write Store)
-- =============================================================================
-- Executar como superuser (postgres). Ajuste o nome do DB e do usuário se necessário.
-- Uso: psql -U postgres -f 00_init_database.sql
-- =============================================================================

-- Criar usuário (ignora erro se já existir)
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'admin') THEN
    CREATE ROLE admin WITH LOGIN PASSWORD 'admin_password';
  END IF;
END
$$;

-- Criar banco (conecte-se antes a um DB existente, ex.: postgres)
SELECT 'CREATE DATABASE minerva_db OWNER admin ENCODING ''UTF8'''
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'minerva_db')\gexec

-- Conceder privilégios (executar conectado a minerva_db ou via psql -d minerva_db)
\connect minerva_db;

GRANT ALL PRIVILEGES ON DATABASE minerva_db TO admin;
GRANT ALL ON SCHEMA public TO admin;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO admin;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO admin;

-- Habilitar extensão uuid-ossp se for usar gen_random_uuid() em defaults (opcional)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
