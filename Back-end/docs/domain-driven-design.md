# Domain-Driven Design (DDD)

Este documento explica o uso de **Modelo Rico** (Rich Domain), o papel dos **Domain Services** e a proteção das **invariantes** nas entidades do **Minerva Gestão de Pedidos**.

---

## Modelo Rico vs Modelo Anêmico

| Modelo Anêmico | Modelo Rico |
|----------------|-------------|
| Entidades com getters/setters públicos, sem regras. | Entidades com **comportamento** e **invariantes** garantidas no próprio tipo. |
| Regras espalhadas em serviços e handlers. | Regras concentradas na entidade e em Domain Services. |
| Fácil quebrar consistência (ex.: total do pedido incorreto). | Impossível criar ou alterar em estado inválido via API pública da entidade. |

No projeto, as entidades **Order** e **User** seguem **modelo rico**: só podem ser criadas e modificadas por construtores e métodos que garantem as regras.

---

## Entidade Order: invariantes e comportamentos

A entidade **Order** (`Minerva.GestaoPedidos.Domain/Entities/Order.cs`) implementa:

### Criação e regra de ouro

- **Método fábrica** `Order.Create(customerId, paymentConditionId, orderDate, items)`:
  - Valida `customerId`, `paymentConditionId`, `orderDate` e que há pelo menos um item.
  - Cria a raiz e os itens via `AddOrderItem` (que recalcula o total).
  - Aplica a **regra de ouro**: se `TotalAmount > 5000` → `Status = Criado`, `RequiresManualApproval = true`; caso contrário → `Status = Pago`, `RequiresManualApproval = false`.

### Propriedades com private set

- `Id`, `CustomerId`, `PaymentConditionId`, `OrderDate`, `TotalAmount`, `Status`, `RequiresManualApproval`, `CreatedAt`, `IdempotencyKey`, `ApprovedBy`, `ApprovedAt` têm **private set**. Alterações passam por métodos de domínio.

### Comportamentos (métodos de domínio)

- **Approve(approvedBy)**: altera status para Pago, preenche `ApprovedBy` e `ApprovedAt`. Lança se já estiver Pago ou Cancelado.
- **Cancel()**: altera status para Cancelado. Lança se já estiver Pago.
- **SetIdempotencyKey(key)**: define a chave de idempotência (única no banco para evitar duplicatas).

Assim, as **invariantes** (total = soma dos itens, status coerente com aprovação) são protegidas no próprio domínio.

---

## Entidade User: invariantes e comportamentos

A entidade **User** (`Minerva.GestaoPedidos.Domain/Entities/User.cs`) implementa:

### Construtor rico

- **Um único construtor público** para criação: `User(string firstName, string lastName, string email, bool active)`.
- **Validações no construtor**: FirstName/LastName não nulos nem vazios (após trim); Email não nulo, não vazio e formato válido (regex).
- **Efeitos controlados**: email normalizado com `Trim().ToLowerInvariant()`.
- **Construtor protegido** sem parâmetros: reservado ao EF Core; não usado na lógica de domínio.

### Propriedades com private set

- `Id`, `FirstName`, `LastName`, `Email`, `Active`, `RegistrationNumber` têm **private set**.

### Comportamentos (métodos de domínio)

- **Activate()** / **Deactivate()**: alteram `Active` de forma controlada.
- **UpdateName(firstName, lastName)**: atualizam nome e sobrenome com as mesmas regras de não nulo e trim.

---

## Domain Services

Algumas regras dependem de **persistência** (ex.: “email deve ser único”). Essas regras ficam em um **Domain Service**.

### IUserDomainService

- **Interface** no **Domain** (`IUserDomainService`): método `ValidateUniqueEmailAsync(string email, CancellationToken)`.
- **Implementação** na **Infrastructure** (`UserDomainService`): usa `IUserRepository.ExistsByEmailAsync`; se o e-mail já existir, lança **ConflictException**.

O **CreateUserCommandHandler** chama o Domain Service **antes** de instanciar o User:

1. `ValidateUniqueEmailAsync(request.Email)` → garante unicidade.
2. `new User(request.FirstName, request.LastName, request.Email, request.Active)` → garante integridade do agregado.
3. `AddAsync(user)` e fluxo de persistência.

Assim, a regra de “email único” **nunca é ignorada** pelo handler e falha com 422 (Conflict) quando há duplicidade.

---

## Resumo

- **Modelo Rico**: Order (Create, Approve, Cancel, SetIdempotencyKey) e User (construtor que valida, Activate/Deactivate, UpdateName).
- **Domain Service**: `IUserDomainService` para regras que dependem de repositório (unicidade de e-mail).
- **Invariantes**: protegidas no construtor e nos métodos das entidades; erros de negócio retornam 422 via `ConflictException`/`BusinessException` e 409 para idempotência via `OrderAlreadyExistsException`, tratados pelo **GlobalExceptionHandlerMiddleware**.
