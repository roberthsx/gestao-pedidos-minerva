# Revisão Técnica — Minerva Gestão de Pedidos (Back-end)

## Observabilidade

### Health Checks e resiliência da infraestrutura

A API expõe o endpoint **`GET /health`**, que retorna um JSON detalhado com o status de cada dependência (Healthy, Unhealthy, Degraded). Esse endpoint é a base para **Liveness** e **Readiness** em orquestradores (Kubernetes, Docker, etc.) e para monitoramento externo.

#### O que é verificado

| Verificação | Descrição | Tags |
|-------------|-----------|------|
| **self** | API está no ar (Liveness). | `live` |
| **postgres** | Conexão com o PostgreSQL (banco de escrita). | `db`, `ready` |
| **mongodb** | Conexão com o MongoDB (banco de leitura). | `db`, `ready` |
| **redis** | Conexão com o Redis (quando configurado). | `cache`, `ready` |
| **startup_migrations** | Banco acessível e migrações EF aplicadas (ou InMemory ativo). | `ready` |

As connection strings vêm de **variáveis de ambiente** ou **appsettings** (`ConnectionStrings:Postgres`, `ConnectionStrings:Mongo`, `ConnectionStrings:Redis`). Se uma connection string não estiver configurada, a respectiva verificação não é registrada (ex.: Redis opcional).

#### Formato da resposta

A resposta do `/health` usa **`AspNetCore.HealthChecks.UI.Client`** (`WriteHealthCheckUIResponse`), expondo:

- Status global: `Healthy`, `Unhealthy` ou `Degraded`
- Status individual por check
- Detalhes e mensagens de erro quando houver

#### Liveness e Readiness (probes)

- **Liveness**  
  Responde à pergunta: “O processo está vivo?”  
  Uso típico: chamar um endpoint leve (ex.: `/health` filtrando por tag `live`) ou o próprio `/health`. Se falhar, o orquestrador pode reiniciar o contêiner/pod.

- **Readiness**  
  Responde à pergunta: “O processo está pronto para receber tráfego?”  
  Uso típico: chamar `/health` considerando apenas checks que indicam “pronto para uso” (ex.: banco e migrações). Em Kubernetes, um pod só entra no Service quando o readiness probe passa.

No projeto, todos os checks estão no mesmo endpoint; em Kubernetes pode-se usar:

- **Liveness probe**: `httpGet http://:8080/health` (ou um endpoint que só faça o check `self`).
- **Readiness probe**: `httpGet http://:8080/health` — quando qualquer dependência crítica (Postgres, Mongo, startup_migrations) estiver Unhealthy, o status global fica Unhealthy e o pod pode ser retirado do balanceamento.

Assim, os Health Checks ajudam na **resiliência da infraestrutura**: evita-se enviar tráfego para instâncias que ainda não estão prontas ou cujas dependências estão indisponíveis, e o orquestrador pode reiniciar instâncias que deixaram de responder.

#### Docker

O **Dockerfile** inclui a instrução **HEALTHCHECK** apontando para `http://localhost:8080/health`. O `docker run` e o `docker-compose` usam esse check para marcar o contêiner como healthy/unhealthy.
