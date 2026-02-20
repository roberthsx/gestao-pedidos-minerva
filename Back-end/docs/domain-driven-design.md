# Domain-Driven Design (DDD)

Este documento explica o uso de **Modelo Rico** (Rich Domain), o papel dos **Domain Services** e a proteção das **invariantes** na entidade User.

---

## Modelo Rico vs Modelo Anêmico

| Modelo Anêmico | Modelo Rico |
|----------------|-------------|
| Entidades com getters/setters públicos, sem regras. | Entidades com **comportamento** e **invariantes** garantidas no próprio tipo. |
| Regras espalhadas em serviços e handlers. | Regras concentradas na entidade e em Domain Services. |
| Fácil quebrar consistência (ex.: email vazio). | Impossível criar ou alterar em estado inválido via API pública da entidade. |

No template, a entidade **User** é um **modelo rico**: só pode ser criada e modificada por construtor e métodos que garantem as regras.

---

## Entidade User: invariantes e comportamentos

A entidade **User** (`MyProject.Domain/Entities/User.cs`) implementa:

### Construtor rico

- **Um único construtor público** para criação: `User(string firstName, string lastName, string email, bool active)`.
- **Validações no construtor**:
  - `FirstName` e `LastName`: não nulos nem vazios (após trim).
  - `Email`: não nulo, não vazio e **formato válido** (regex).
- **Efeitos colaterais controlados**: `Id = Guid.NewGuid()`, email normalizado com `Trim().ToLowerInvariant()`.
- **Construtor protegido** sem parâmetros: reservado ao EF Core para materialização; não deve ser usado na lógica de domínio.

### Propriedades com private set

- `Id`, `FirstName`, `LastName`, `Email`, `Active` têm **private set**. Ninguém altera estado “por fora”; apenas a própria entidade e o EF (via reflexão) podem atribuir.

### Comportamentos (métodos de domínio)

- **Activate()** / **Deactivate()**: alteram `Active` de forma controlada.
- **UpdateName(firstName, lastName)**: atualizam nome e sobrenome com as **mesmas regras** de não nulo e trim.

Assim, as **invariantes** (nome e email válidos, email normalizado) são protegidas no próprio domínio.

---

## Domain Services

Algumas regras dependem de **persistência** (ex.: “email deve ser único”). Essas regras não cabem na entidade sozinha; ficam em um **Domain Service**.

### IUserDomainService

- **Interface** no **Domain** (`IUserDomainService`): método `ValidateUniqueEmailAsync(string email, CancellationToken)`.
- **Implementação** na **Infrastructure** (`UserDomainService`): usa `IUserRepository.ExistsByEmailAsync`; se o e-mail já existir, lança **ConflictException**.

O **CreateUserCommandHandler** chama o Domain Service **antes** de instanciar o User:

1. `ValidateUniqueEmailAsync(request.Email)` → garante unicidade.
2. `new User(request.FirstName, request.LastName, request.Email, request.Active)` → garante integridade do agregado.
3. `AddAsync(user)` e publicação do evento.

Assim, a regra de “email único” **nunca é ignorada** pelo handler: ela é executada sempre que um usuário é criado e falha com 422 (Conflict) quando há duplicidade.

---

## Resumo

- **Modelo Rico**: User com construtor que valida e métodos que encapsulam mudanças.
- **Domain Service**: `IUserDomainService` para regras que dependem de repositório (ex.: unicidade de e-mail).
- **Invariantes**: protegidas no construtor e nos métodos da entidade; erros de negócio (conflito) retornam 422 via `ConflictException` e handler global.
