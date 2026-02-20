# Minerva Gestão de Pedidos — Documentação das APIs

Base URL (exemplo): `https://localhost:7xxx/api` ou `http://localhost:5xxx/api`

---

## Autenticação

A maioria dos endpoints exige **Bearer Token** no header:

```http
Authorization: Bearer {accessToken}
```

Exceções (públicas): `POST /api/auth/login`, `GET /api/users` (conforme implementação atual).

---

## 1. Autenticação (Auth)

### 1.1 Login

**POST** `/api/auth/login`  
**Autenticação:** não exigida

**Request (body – JSON):**

| Campo     | Tipo   | Obrigatório | Descrição                          |
|-----------|--------|-------------|------------------------------------|
| matricula | string | Sim         | Matrícula do usuário                |
| senha     | string | Sim         | Senha em texto puro                 |

**Exemplo:**

```json
{
  "matricula": "1001",
  "senha": "Senha@123"
}
```

**Response 200 (sucesso):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "user": {
    "nome": "Admin Sistema",
    "perfil": "MANAGER"
  }
}
```

**Response 401:** credenciais inválidas  
Body: `{ "error": "Matrícula ou senha inválidos." }`

---

## 2. Usuários (Users)

### 2.1 Criar usuário

**POST** `/api/users`  
**Autenticação:** conforme controller (verifique se está com `[Authorize]`)

**Request (body – JSON):**

| Campo     | Tipo   | Obrigatório | Descrição   |
|-----------|--------|-------------|-------------|
| firstName | string | Sim         | Nome        |
| lastName  | string | Sim         | Sobrenome   |
| email     | string | Sim         | E-mail      |
| active    | bool   | Sim         | Ativo       |

**Exemplo:**

```json
{
  "firstName": "Maria",
  "lastName": "Silva",
  "email": "maria.silva@minerva.com",
  "active": true
}
```

**Response 201:** retorna `UserDto` e header `Location` apontando para `GET /api/users/{id}`.

---

### 2.2 Obter usuário por ID

**GET** `/api/users/{id}`  
**Autenticação:** conforme controller

**Parâmetros de rota:**

| Nome | Tipo | Descrição   |
|------|------|-------------|
| id   | int  | ID do usuário |

**Response 200:** `UserDto`

```json
{
  "id": 1,
  "firstName": "Maria",
  "lastName": "Silva",
  "email": "maria.silva@minerva.com",
  "active": true
}
```

**Response 404:** usuário não encontrado.

---

### 2.3 Listar todos os usuários

**GET** `/api/users`  
**Autenticação:** conforme controller

**Response 200:** array de `UserDto`

```json
[
  {
    "id": 1,
    "firstName": "Maria",
    "lastName": "Silva",
    "email": "maria.silva@minerva.com",
    "active": true
  }
]
```

---

## 3. Pedidos (Orders)

Todos os endpoints de pedidos exigem **Bearer Token** (`[Authorize]`).  
Aprovação exige perfil **MANAGER** (`[Authorize(Roles = "MANAGER")]`).

---

### 3.1 Listar pedidos (GET paginado)

**GET** `/api/orders`  
**Autenticação:** Bearer Token obrigatório

Consulta feita **somente na camada de leitura (MongoDB)**.

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
GET /api/orders
GET /api/orders?status=Pago
GET /api/orders?dateFrom=2025-01-01&dateTo=2025-01-31
GET /api/orders?pageNumber=2&pageSize=10
GET /api/orders?status=Criado&pageNumber=1&pageSize=20
```

**Response 200 – contrato de resposta paginada:**

```json
{
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
}
```

| Campo       | Tipo   | Descrição                    |
|-------------|--------|------------------------------|
| items       | array  | Lista de `OrderDto` da página |
| totalCount  | int    | Total de registros (sem paginação) |
| pageNumber  | int    | Página atual                 |
| pageSize    | int    | Tamanho da página            |

---

### 3.2 Criar pedido

**POST** `/api/orders`  
**Autenticação:** Bearer Token obrigatório

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

**Response 201:** retorna `OrderDto` do pedido criado e header `Location` para a listagem (ex.: `GET /api/orders`).

**Regras de negócio (resumo):**  
- Total > 5000: `status = Criado`, `requiresManualApproval = true`.  
- Total ≤ 5000: `status = Pago`, `requiresManualApproval = false`.

**Erros:** 400 (validação), 404 (cliente ou condição de pagamento não encontrados).

---

### 3.3 Aprovar pedido

**PUT** `/api/orders/{orderId}/approve`  
**Autenticação:** Bearer Token com perfil **MANAGER**

**Parâmetros de rota:**

| Nome    | Tipo | Descrição   |
|---------|------|-------------|
| orderId | int  | ID do pedido |

**Request:** sem body.

**Exemplo:**

```http
PUT /api/orders/1/approve
Authorization: Bearer {token}
```

**Response 200:** retorna `OrderDto` do pedido já com `status = "Pago"`.

**Erros:**

| Status | Situação                          | Exemplo de mensagem |
|--------|-----------------------------------|----------------------|
| 400    | Pedido já pago                    | "Cannot approve order: order is already paid." |
| 400    | Pedido cancelado                  | "Cannot approve order: order is canceled." |
| 400    | Não exige aprovação manual        | "Order does not require manual approval." |
| 403    | Usuário sem perfil MANAGER        | - |
| 404    | Pedido não encontrado             | "Order '1' was not found." |

---

## 4. Contrato padrão de resposta paginada

Para qualquer **GET paginado** na API, use o mesmo contrato:

```json
{
  "items": [ /* array de DTOs da entidade */ ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 20
}
```

- **items:** registros da página atual.  
- **totalCount:** total de registros que atendem ao filtro (para calcular total de páginas: `Math.Ceil(totalCount / pageSize)`).  
- **pageNumber:** página solicitada (1-based).  
- **pageSize:** quantidade de itens por página.

**Back-end:** o tipo genérico usado é `PagedResponse<T>` (ex.: `GET /api/orders` retorna `PagedResponse<OrderDto>`). Todas as listagens paginadas devem retornar esse mesmo contrato.

**Exemplo de uso no front-end (JavaScript/TypeScript):**

```typescript
// Tipo genérico para qualquer listagem paginada
interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// GET /api/orders?pageNumber=1&pageSize=10
const totalPages = Math.ceil(response.totalCount / response.pageSize);
const hasNext = response.pageNumber < totalPages;
```

---

## 5. Tratamento de erros (RFC 7807)

Em caso de erro, a API pode retornar **Problem Details** (JSON):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Cannot approve order: order is already paid.",
  "instance": "/api/orders/1/approve"
}
```

Em **Development**, o corpo pode incluir `stackTrace` e `errors` (validação).

---

## 6. Resumo dos endpoints

| Método | Rota                          | Auth     | Descrição              |
|--------|--------------------------------|----------|------------------------|
| POST   | /api/auth/login               | Não      | Login (matrícula/senha) |
| POST   | /api/users                    | Conforme controller | Criar usuário |
| GET    | /api/users/{id}               | Conforme controller | Usuário por ID |
| GET    | /api/users                    | Conforme controller | Listar usuários |
| GET    | /api/orders                   | Bearer   | Listar pedidos (paginado, MongoDB) |
| POST   | /api/orders                   | Bearer   | Criar pedido           |
| PUT    | /api/orders/{orderId}/approve | Bearer + MANAGER | Aprovar pedido |

---

*Documento alinhado aos controllers e DTOs do projeto Minerva.GestaoPedidos.WebApi.*
