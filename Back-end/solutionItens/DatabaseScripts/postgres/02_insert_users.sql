-- =============================================================================
-- Minerva.GestaoPedidos - Inserts de usuários (tabela Users)
-- =============================================================================
-- Executar APÓS 02_insert_profiles.sql (perfis 1=ADMIN, 2=GESTAO, 3=ANALISTA).
-- Senha padrão para todos: Senha@123 (hash BCrypt).
-- Para gerar outro hash: dotnet run (pasta solutionItens/DatabaseScripts/postgres/gen_password_hash)
-- =============================================================================

INSERT INTO "Users" ("FirstName", "LastName", "Email", "Active", "ProfileId", "Matricula", "PasswordHash")
VALUES
    ('Admin', 'Sistema', 'admin@minerva.com', true, 1, '1001', '$2a$11$ytwaU25boVCJoAq4In9VzerpCFj3id9zHYCxMjnBBqzP0u6/Y/aZS'),
    ('Maria', 'Silva', 'maria.silva@minerva.com', true, 2, '1002', '$2a$11$ytwaU25boVCJoAq4In9VzerpCFj3id9zHYCxMjnBBqzP0u6/Y/aZS'),
    ('João', 'Santos', 'joao.santos@minerva.com', true, 2, '1003', '$2a$11$ytwaU25boVCJoAq4In9VzerpCFj3id9zHYCxMjnBBqzP0u6/Y/aZS'),
    ('Ana', 'Costa', 'ana.costa@minerva.com', true, 3, '1004', '$2a$11$ytwaU25boVCJoAq4In9VzerpCFj3id9zHYCxMjnBBqzP0u6/Y/aZS'),
    ('Carlos', 'Oliveira', 'carlos.oliveira@minerva.com', true, 3, '1005', '$2a$11$ytwaU25boVCJoAq4In9VzerpCFj3id9zHYCxMjnBBqzP0u6/Y/aZS');
