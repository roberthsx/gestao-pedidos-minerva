# Minerva — Gestão de Pedidos

Solução completa de **gestão de pedidos** composta por **back-end** (API .NET 8 + Worker) e **front-end** (React/Vite). Este repositório contém os dois projetos em pastas separadas, cada uma com sua própria documentação e formas de execução (Docker ou local).

---

## Estrutura do repositório

| Pasta | Descrição |
|-------|------------|
| **Back-end/** | API REST, Worker, PostgreSQL, Kafka — Clean Architecture, CQRS. |
| **Front/** | Aplicação React (Vite, TypeScript, Tailwind) que consome a API. |

Para **configurar e subir** cada parte da solução, utilize os READMEs dentro de cada pasta. Eles trazem pré-requisitos, comandos e variáveis de ambiente.

---

## Como subir a solução

### 1. Back-end

Abra o README do back-end para instruções detalhadas:

- **[Back-end/README.md](./Back-end/README.md)**

Resumo rápido:

- **Com Docker (recomendado)**  
  Na pasta `Back-end/`, execute `docker compose up -d`. Sobem API (porta **5002**), Worker, PostgreSQL e Kafka. Swagger: [http://localhost:5002/swagger](http://localhost:5002/swagger).

- **Local (sem Docker para API/Worker)**  
  Com Postgres e Kafka rodando (ex.: só os serviços de infra via Docker), use `dotnet run` nos projetos WebApi e Worker conforme indicado no README do Back-end.

### 2. Front-end

Abra o README do front-end para instruções detalhadas:

- **[Front/README.md](./Front/README.md)**

Resumo rápido:

- **Com Docker**  
  Na pasta `Front/`, execute `docker-compose up -d --build`. A aplicação fica em [http://localhost:3000](http://localhost:3000). Por padrão, a API é esperada em `http://localhost:5002/api`.

- **Local**  
  Na pasta `Front/`, configure `.env` com `VITE_API_URL=http://localhost:5002/api`, execute `npm install` e `npm run dev`. Acesse a URL exibida no terminal (ex.: `http://localhost:5173`).

---

## Ordem sugerida para subir tudo

1. **Subir o back-end** (API + infra: Postgres, Kafka) — ver [Back-end/README.md](./Back-end/README.md).  
2. **Subir o front-end** (Docker ou local) — ver [Front/README.md](./Front/README.md).  
3. Acessar o front (ex.: `http://localhost:3000` ou `http://localhost:5173`) e usar a aplicação; o front chama a API na porta **5002**.

Para detalhes de portas, variáveis de ambiente, testes e troubleshooting, consulte sempre o **README de cada pasta**.

---