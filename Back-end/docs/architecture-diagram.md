# Diagrama de Arquitetura — CQRS com Persistência Poliglota

Diagrama da solução **Template 1**: CQRS, Write Stack (PostgreSQL) e Read Stack (MongoDB), com sincronização **síncrona** via Domain Events.

---

## Visão geral do fluxo

```mermaid
flowchart TD
    subgraph Client["🖥️ Cliente Externo"]
        APP[Web / Mobile App]
    end

    subgraph API["🌐 Borda da API"]
        CTRL[ASP.NET Core Web API\nControllers]
    end

    subgraph AppLayer["📦 Camada de Aplicação (MediatR)"]
        direction TB
        subgraph WriteFlow["Fluxo de Escrita (Commands)"]
            direction TB
            MEDIATR_CMD[MediatR]
            PIPE_LOG[Pipeline: LoggingBehavior]
            PIPE_VAL[Pipeline: ValidationBehavior]
            CMD_HANDLER[CreateUserCommandHandler]
            MEDIATR_CMD --> PIPE_LOG --> PIPE_VAL --> CMD_HANDLER
        end
        subgraph ReadFlow["Fluxo de Leitura (Queries)"]
            direction TB
            MEDIATR_Q[MediatR]
            Q_HANDLER[GetUserByIdQueryHandler\nGetUsersQueryHandler]
            MEDIATR_Q --> Q_HANDLER
        end
    end

    subgraph DomainLayer["⚙️ Camada de Domínio"]
        direction TB
        DOM_SVC[Domain Service\nIUserDomainService\nValidateUniqueEmailAsync]
        RICH_ENTITY[Rich Entity\nUser\nconstrutor + invariantes]
        DOM_EVENT[Domain Event\nUserCreatedEvent]
        DOM_SVC --> RICH_ENTITY
        CMD_HANDLER --> DOM_SVC
        CMD_HANDLER --> RICH_ENTITY
        CMD_HANDLER --> DOM_EVENT
    end

    subgraph InfraWrite["🔵 Infraestrutura — Write Side"]
        direction TB
        REPO[EF Core Repository\nIUserRepository]
        POSTGRES[(PostgreSQL)]
        REPO --> POSTGRES
    end

    subgraph Sync["🔄 Sincronização (síncrona)"]
        SYNC_HANDLER[SyncUserToMongoHandler\nINotificationHandler]
        DOM_EVENT --> SYNC_HANDLER
    end

    subgraph InfraRead["🟢 Infraestrutura — Read Side"]
        direction TB
        MONGO_REPO[MongoDB Repository\nIUserReadRepository]
        MONGODB[(MongoDB)]
        MONGO_REPO --> MONGODB
        SYNC_HANDLER --> MONGODB
    end

    APP -->|HTTP Request| CTRL
    CTRL -->|Command| WriteFlow
    CTRL -->|Query| ReadFlow
    CMD_HANDLER --> REPO
    RICH_ENTITY --> REPO
    Q_HANDLER --> MONGO_REPO
    CTRL <-->|HTTP Response| APP

    style POSTGRES fill:#4a90d9,color:#fff
    style MONGODB fill:#5cb85c,color:#fff
```

---

## Fluxo crítico: Command com sincronização síncrona

O diagrama abaixo detalha o **fluxo síncrono** de um Command: escrita no Postgres, disparo do Domain Event, gravação no MongoDB pelo Sync Handler e só então a resposta HTTP ao cliente.

```mermaid
flowchart TD
    subgraph Client["Cliente"]
        A[Web/Mobile App]
    end

    subgraph API["API"]
        B[UsersController\nPOST /api/users]
    end

    subgraph Pipeline["Pipeline MediatR"]
        C[LoggingBehavior]
        D[ValidationBehavior]
        C --> D
    end

    subgraph Handler["Command Handler"]
        E[CreateUserCommandHandler]
    end

    subgraph Domain["Domínio"]
        F[IUserDomainService\nValidateUniqueEmailAsync]
        G[User\nRich Entity]
        H[UserCreatedEvent\npublicado]
        E --> F
        E --> G
        G --> I
        E --> H
    end

    subgraph Write["Write Stack"]
        I[IUserRepository.AddAsync]
        J[(PostgreSQL)]
        I --> J
    end

    subgraph Sync["Sync (mesmo fluxo)"]
        K[SyncUserToMongoHandler\nHandle UserCreatedEvent]
        L[(MongoDB)]
        H --> K
        K --> L
    end

    subgraph Response["Resposta"]
        M[201 Created\nUserDto]
    end

    A -->|1. POST| B
    B -->|2. Send Command| Pipeline
    Pipeline --> E
    E -->|3. Valida email único| F
    E -->|4. new User| G
    E -->|5. AddAsync| I
    J -->|6. Sucesso| E
    E -->|7. Publish Event| H
    H -->|8. Handler grava no Mongo| K
    L -->|9. Sync concluído| E
    E -->|10. Retorna DTO| M
    M -->|11. HTTP Response| A

    style J fill:#4a90d9,color:#fff
    style L fill:#5cb85c,color:#fff
```

---

## Legenda

| Cor / elemento | Significado |
|----------------|-------------|
| **Azul** | Banco relacional (PostgreSQL) — Write Side |
| **Verde** | Banco NoSQL (MongoDB) — Read Side |
| **Fluxo contínuo** | Command → Postgres → Event → Sync Handler → MongoDB → Response (tudo no mesmo request, síncrono) |
| **Queries** | Apenas leem do MongoDB via IUserReadRepository; não passam pelo Write Stack |

---

## Referência

- [architecture.md](./architecture.md) — Camadas e Clean Architecture  
- [persistence-polyglot.md](./persistence-polyglot.md) — Persistência poliglota e Domain Events  
- [cqrs-mediatr.md](./cqrs-mediatr.md) — CQRS e MediatR Pipelines  
