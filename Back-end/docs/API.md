# Minerva Gestão de Pedidos — Documentação das APIs

Base URL (exemplo): `https://localhost:7xxx/api/v1` ou `http://localhost:5xxx/api/v1`

---

## Autenticação

A maioria dos endpoints exige **Bearer Token** no header:

```http
Authorization: Bearer {accessToken}
```

Exceções (públicas): `POST /api/v1/auth/login`.

Obtenha o token em **POST /api/v1/auth/login** (número de registro e senha).

---

## Envelope de resposta (ApiResponse)

As respostas 2xx e os erros retornam o envelope padronizado:

```json
{
  "success": true,
  "data": { ... },
  "message": null,
  "errors": null
}
```

Em erro: `success: false`, `data: null`, `message` e `errors` preenchidos conforme o tipo de falha.

---

## 1. Autenticação (Auth)

### 1.1 Login

**POST** `/api/v1/auth/login`  
**Autenticação:** não exigida

**Request (body – JSON):**

| Campo             | Tipo   | Obrigatório | Descrição                          |
|-------------------|--------|-------------|------------------------------------|
| registrationNumber| string | Sim         | Número de registro (matrícula) do usuário |
| senha             | string | Sim         | Senha em texto puro                 |

**Exemplo:**

```json
{
  "registrationNumber": "admin",
  "senha": "Senha@123"
}
```

**Response 200 (sucesso):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "user": {
    "name": "Admin Sistema",
    "role": "ADMIN"
  }
}
```

**Response 401:** credenciais inválidas  
Body: `{ "error": "Matrícula ou senha inválidos." }` (ou mensagem equivalente em PT).

---

## 2. Usuários (Users)

Todos os endpoints de usuários exigem **Bearer Token** (`[Authorize]`).

### 2.1 Criar usuário

**POST** `/api/v1/users`

**Request (body – JSON):**

| Campo     | Tipo   | Obrigatório | Descrição   |
|-----------|--------|-------------|-------------|
| firstName | string | Sim         | Nome        |
| lastName  | string | Sim         | Sobrenome   |
| email     | string | Sim         | E-mail      |
| active    | bool   | Sim         | Ativo       |

**Response 201:** retorna `UserDto` no envelope e header `Location` apontando para `GET /api/v1/users/{id}`.

---

### 2.2 Obter usuário por ID

**GET** `/api/v1/users/{id}`

**Parâmetros de rota:**

| Nome | Tipo | Descrição   |
|------|------|-------------|
| id   | int  | ID do usuário |

**Response 200:** `UserDto` no envelope.

**Response 404:** usuário não encontrado.

---

### 2.3 Listar todos os usuários

**GET** `/api/v1/users`

**Response 200:** array de `UserDto` no envelope.

---

## 3. Clientes (Customers) — Lookup

**GET** `/api/v1/customers`  
**Autenticação:** Bearer Token obrigatório

Lista todos os clientes (Id, Name) para dropdowns e seleção em pedidos.

**Response 200:** array de `CustomerLookupDto` no envelope.

```json
{
  "success": true,
  "data": [
    { "id": 1, "name": "Minerva Foods" }
  ],
  "message": null,
  "errors": null
}
```

---

## 4. Condições de pagamento (Payment Conditions) — Lookup

**GET** `/api/v1/payment-conditions`  
**Autenticação:** Bearer Token obrigatório

Lista todas as condições de pagamento para dropdowns.

**Response 200:** array de `PaymentConditionLookupDto` no envelope.

| Campo                | Tipo  | Descrição                    |
|----------------------|-------|------------------------------|
| id                   | int   | ID da condição               |
| description          | string| Descrição (ex.: "30/60/90")  |
| numberOfInstallments | int   | Número de parcelas           |

---

## 5. Pedidos (Orders)

Todos os endpoints de pedidos exigem **Bearer Token** (`[Authorize]`).  
**Criar pedido** exige perfil **ADMIN** ou **MANAGER**.  
**Aprovar pedido** exige perfil **ADMIN**, **MANAGER** ou **ANALYST**.

---

### 5.1 Listar pedidos (GET paginado)

**GET** `/api/v1/orders`  
**Autenticação:** Bearer Token obrigatório

Consulta feita na camada de leitura (**PostgreSQL**, `IOrderReadRepository`).

**Query parameters:**

| Nome       | Tipo    | Obrigatório | Default | Descrição                          |
|------------|---------|-------------|---------|------------------------------------|
| status     | string  | Não         | -       | Filtrar por status (ex.: `Criado`, `Pago`, `Cancelado`) |
| dateFrom   | datetime| Não         | -       | Data do pedido (início) – ISO 8601 |
| dateTo     | datetime| Não         | -       | Data do pedido (fim) – ISO 8601    |
| pageNumber | int     | Não         | 1       | Número da página                   |
| pageSize   | int     | Não         | 20      | Itens por página                   |

**Exemplos de URL:**

```http
GET /api/v1/orders
GET /api/v1/orders?status=Pago
GET /api/v1/orders?dateFrom=2025-01-01&dateTo=2025-01-31
GET /api/v1/orders?pageNumber=2&pageSize=10
```

**Response 200 – contrato de resposta paginada (em `data`):**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "customerId": 1,
        "customerName": "Minerva Foods",
        "paymentConditionId": 1,
        "paymentConditionDescription": "30/60/90",
        "orderDate": "2025-02-10T14:30:00Z",
        "totalAmount": 6000.00,
        "status": "Criado",
        "requiresManualApproval": true,
        "deliveryDays": 0,
        "estimatedDeliveryDate": null,
        "approvedBy": null,
        "approvedAt": null,
        "items": [
          {
            "productName": "Produto A",
            "quantity": 10,
            "unitPrice": 600.00,
            "totalPrice": 6000.00
          }
        ]
      }
    ],
    "totalCount": 42,
    "pageNumber": 1,
    "pageSize": 20
  },
  "message": null,
  "errors": null
}
```

| Campo       | Tipo   | Descrição                    |
|-------------|--------|------------------------------|
| items       | array  | Lista de `OrderDto` da página |
| totalCount  | int    | Total de registros (sem paginação) |
| pageNumber  | int    | Página atual                 |
| pageSize    | int    | Tamanho da página            |

---

### 5.2 Criar pedido

**POST** `/api/v1/orders`  
**Autenticação:** Bearer Token com perfil **ADMIN** ou **MANAGER** (403 para ANALYST)

**Request (body – JSON):**

| Campo             | Tipo   | Obrigatório | Descrição                |
|-------------------|--------|-------------|---------------------------|
| customerId        | int    | Sim         | ID do cliente             |
| paymentConditionId| int    | Sim         | ID da condição de pagamento |
| orderDate         | datetime? | Não      | Data do pedido (default: UTC now) |
| items             | array  | Sim         | Lista de itens (mín. 1)   |

Cada item em `items`:

| Campo       | Tipo   | Obrigatório | Descrição        |
|-------------|--------|-------------|------------------|
| productName | string | Sim         | Nome do produto  |
| quantity    | int    | Sim         | Quantidade (> 0) |
| unitPrice   | decimal| Sim         | Preço unitário (> 0) |

**Exemplo:**

```json
{
  "customerId": 1,
  "paymentConditionId": 1,
  "orderDate": "2025-02-10T14:00:00Z",
  "items": [
    {
      "productName": "Produto A",
      "quantity": 10,
      "unitPrice": 600.00
    },
    {
      "productName": "Produto B",
      "quantity": 5,
      "unitPrice": 100.00
    }
  ]
}
```

**Response 201:** retorna `OrderDto` no envelope e header `Location` para a listagem (ex.: `GET /api/v1/orders`).

**Regras de negócio (resumo):**  
- Total > 5000: `status = Criado`, `requiresManualApproval = true`.  
- Total ≤ 5000: `status = Pago`, `requiresManualApproval = false`.

A API publica no Kafka (tópico `order-created`) para processamento assíncrono (ex.: criação de prazo de entrega no Worker). Em duplicata (idempotência), retorna **409 Conflict**.

**Erros:** 400 (validação), 403 (ANALYST não pode criar), 404 (cliente ou condição de pagamento não encontrados), 409 (pedido já processado/idempotência).

---

### 5.3 Aprovar pedido

**PUT** `/api/v1/orders/{orderId}/approve`  
**Autenticação:** Bearer Token com perfil **ADMIN**, **MANAGER** ou **ANALYST**

**Parâmetros de rota:**

| Nome    | Tipo | Descrição   |
|---------|------|-------------|
| orderId | int  | ID do pedido |

**Request:** sem body.

**Response 200:** retorna `OrderDto` no envelope, com `status = "Pago"`, `approvedBy` (matrícula) e `approvedAt` preenchidos.

**Erros:**

| Status | Situação                          | Exemplo de mensagem |
|--------|-----------------------------------|----------------------|
| 400    | Pedido já pago                    | "Order is already paid." |
| 400    | Pedido cancelado                  | "Cannot approve a canceled order." |
| 400    | Não exige aprovação manual        | (regra de negócio) |
| 403    | Usuário sem perfil permitido      | - |
| 404    | Pedido não encontrado             | "Order '1' was not found." |

---

## 6. Contrato padrão de resposta paginada

Para **GET /api/v1/orders**, o `data` segue o contrato `PagedResponse<T>`:

```json
{
  "items": [ /* array de OrderDto */ ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 20
}
```

- **items:** registros da página atual.  
- **totalCount:** total de registros que atendem ao filtro.  
- **pageNumber:** página solicitada (1-based).  
- **pageSize:** quantidade de itens por página.

**Exemplo de uso no front-end (TypeScript):**

```typescript
interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

const totalPages = Math.ceil(response.data.totalCount / response.data.pageSize);
const hasNext = response.data.pageNumber < totalPages;
```

---

## 7. Tratamento de erros (envelope ApiResponse)

Em caso de erro, a API retorna **envelope ApiResponse** com `success: false`:

```json
{
  "success": false,
  "data": null,
  "message": "Um ou mais erros de validação ocorreram.",
  "errors": ["'Customer Id' must be greater than '0'.", "'Items' must not be empty."]
}
```

Em **Development**, respostas 500 podem incluir detalhes adicionais em `errors` (ex.: stack trace) para facilitar debug.

| Exceção / Situação        | HTTP Status | Uso |
|---------------------------|------------|-----|
| Validação (FluentValidation) | 400        | Request inválido |
| BadRequestException       | 400        | Requisição inválida |
| UnauthorizedAccessException | 401      | Não autorizado |
| NotFoundException         | 404        | Recurso não encontrado |
| OrderAlreadyExistsException (idempotência) | 409 | Pedido já processado |
| ConflictException / BusinessException | 422 | Regra de negócio violada |
| InfrastructureException / ServiceUnavailableException | 503 | Falha de infraestrutura |
| Demais                    | 500        | Erro interno |

---

## 8. Resumo dos endpoints

| Método | Rota                                | Auth     | Descrição              |
|--------|-------------------------------------|----------|------------------------|
| POST   | /api/v1/auth/login                  | Não      | Login (registrationNumber/senha) |
| POST   | /api/v1/users                       | Bearer   | Criar usuário          |
| GET    | /api/v1/users/{id}                  | Bearer   | Usuário por ID         |
| GET    | /api/v1/users                       | Bearer   | Listar usuários        |
| GET    | /api/v1/customers                   | Bearer   | Lookup de clientes     |
| GET    | /api/v1/payment-conditions          | Bearer   | Lookup de condições de pagamento |
| GET    | /api/v1/orders                      | Bearer   | Listar pedidos (paginado, Postgres) |
| POST   | /api/v1/orders                      | Bearer (ADMIN/MANAGER) | Criar pedido |
| PUT    | /api/v1/orders/{orderId}/approve    | Bearer (ADMIN/MANAGER/ANALYST) | Aprovar pedido |

---

## 9. Health e Swagger

| Endpoint        | Descrição |
|-----------------|-----------|
| GET /health/live | Liveness (processo vivo). |
| GET /health      | Readiness (PostgreSQL, Kafka, migrações). |
| /swagger         | Documentação Swagger da API. |

---

*Documento alinhado aos controllers e DTOs do projeto Minerva.GestaoPedidos.WebApi.*
