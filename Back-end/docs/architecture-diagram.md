# Diagrama de Arquitetura — CQRS, PostgreSQL e Kafka

Diagrama da solução **Minerva Gestão de Pedidos**: CQRS, persistência em **PostgreSQL** (escrita e leitura), e eventos assíncronos via **Kafka** (order-created, order-approved), com **Worker** consumindo `order-created` para criar DeliveryTerm.

---

## Visão geral do fluxo

```mermaid
flowchart TD
    subgraph Client["🖥️ Cliente Externo"]
        APP[Web / Mobile App]
    end

    subgraph API["🌐 WebApi"]
        CTRL[ASP.NET Core Web API\nControllers\napi/v1]
    end

    subgraph AppLayer["📦 Camada de Aplicação (MediatR)"]
        direction TB
        subgraph WriteFlow["Fluxo de Escrita (Commands)"]
            MEDIATR_CMD[MediatR]
            PIPE_LOG[Pipeline: LoggingBehavior]
            PIPE_VAL[Pipeline: ValidationBehavior]
            CMD_ORDER[CreateOrderCommandHandler\nApproveOrderCommandHandler]
            MEDIATR_CMD --> PIPE_LOG --> PIPE_VAL --> CMD_ORDER
        end
        subgraph ReadFlow["Fluxo de Leitura (Queries)"]
            MEDIATR_Q[MediatR]
            Q_HANDLER[GetOrdersPagedQueryHandler\nGetUserById, GetUsers, etc.]
            MEDIATR_Q --> Q_HANDLER
        end
    end

    subgraph DomainLayer["⚙️ Camada de Domínio"]
        ORDER_ENTITY[Order\nCreate, Approve, Cancel]
        USER_ENTITY[User, Customer, PaymentCondition]
        DOM_EVENT[Domain Events\nOrderApprovedEvent]
        CMD_ORDER --> ORDER_ENTITY
        CMD_ORDER --> DOM_EVENT
    end

    subgraph Infra["🔵 Infraestrutura"]
        REPO_W[IOrderRepository, IUserRepository, ...]
        REPO_R[IOrderReadRepository, IUserReadRepository, ...]
        KAFKA_PUB[Kafka: order-created\norder-approved]
        POSTGRES[(PostgreSQL)]
        REPO_W --> POSTGRES
        REPO_R --> POSTGRES
        CMD_ORDER --> KAFKA_PUB
    end

    subgraph Worker["🟠 Worker"]
        KAFKA_CONS[OrderCreatedKafkaConsumerHostedService]
        DELIVERY[DeliveryTerm criado\nidempotente]
        KAFKA_CONS --> DELIVERY
        DELIVERY --> POSTGRES
    end

    APP -->|HTTP Request| CTRL
    CTRL -->|Command| WriteFlow
    CTRL -->|Query| ReadFlow
    CMD_ORDER --> REPO_W
    Q_HANDLER --> REPO_R
    KAFKA_PUB -->|tópico order-created| KAFKA_CONS
    CTRL <-->|HTTP Response| APP

    style POSTGRES fill:#4a90d9,color:#fff
```

---

## Fluxo: Criar pedido e evento Kafka

1. Cliente envia **POST /api/v1/orders**.
2. Controller envia **CreateOrderCommand** ao MediatR (Logging → Validation → Handler).
3. **CreateOrderCommandHandler** valida cliente/condição de pagamento, cria **Order** (domínio), persiste via **IOrderRepository** (PostgreSQL).
4. Handler publica no Kafka (**order-created**) com dados do pedido.
5. Resposta 201 com OrderDto ao cliente.
6. **Worker** consome `order-created`, cria **DeliveryTerm** (10 dias) no Postgres (idempotente por OrderId).

---

## Fluxo: Aprovar pedido

1. Cliente envia **PUT /api/v1/orders/{id}/approve** (perfil ADMIN/MANAGER/ANALYST).
2. **ApproveOrderCommandHandler** carrega Order, chama `Order.Approve(approvedBy)`, persiste e publica no Kafka (**order-approved**).
3. Resposta 200 com OrderDto (status Pago, approvedBy, approvedAt).

---

## Legenda

| Elemento | Significado |
|----------|-------------|
| **PostgreSQL** | Único banco transacional: escrita e leitura (repositórios com AsNoTracking para queries). |
| **Kafka** | Eventos order-created (Worker cria DeliveryTerm) e order-approved (integrações externas). |
| **Worker** | Processo separado; consome order-created, persiste DeliveryTerm; retry e DLQ em falha. |

---

## Referência

- [architecture.md](./architecture.md) — Camadas e Clean Architecture  
- [persistence-polyglot.md](./persistence-polyglot.md) — Persistência e Kafka  
- [cqrs-mediatr.md](./cqrs-mediatr.md) — CQRS e MediatR Pipelines  
