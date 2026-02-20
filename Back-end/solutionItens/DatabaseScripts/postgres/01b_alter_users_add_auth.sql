-- =============================================================================
-- Migração: adicionar Matricula e PasswordHash à tabela Users (bancos já existentes)
-- =============================================================================
-- Executar se a tabela "Users" já existir sem essas colunas (ex.: após 01_schema.sql antigo).
-- =============================================================================

ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Matricula" VARCHAR(20) NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordHash" VARCHAR(255) NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Matricula" ON "Users" ("Matricula") WHERE "Matricula" IS NOT NULL;
