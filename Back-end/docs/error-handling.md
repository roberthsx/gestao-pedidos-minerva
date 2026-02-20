# Tratamento de Erros

Este documento descreve o uso do padrão **RFC 7807 (Problem Details)**, do **IExceptionHandler** (.NET 8+) e a escolha do status **422** para erros de negócio.

---

## RFC 7807 (Problem Details)

O template adota **Problem Details for HTTP APIs** (RFC 7807 / RFC 9457) para respostas de erro: um JSON padronizado com campos como `type`, `title`, `status`, `detail`, `instance` e extensões customizadas.

Exemplo (validação):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request is invalid.",
  "instance": "/api/users",
  "errors": {
    "Email": ["'Email' is required."],
    "FirstName": ["'First Name' must not be empty."]
  }
}
```

Benefícios: **contrato estável** para clientes, **machine-readable** e **extensível** via `extensions` (ex.: `errors`, `stackTrace` em Development).

---

## IExceptionHandler (.NET 8+)

Em vez de um middleware manual, o template usa o **IExceptionHandler**:

- **GlobalExceptionHandler** (`WebApi/Handlers/GlobalExceptionHandler.cs`) implementa `IExceptionHandler`.
- Registro: `AddExceptionHandler<GlobalExceptionHandler>()` e `AddProblemDetails()` no `Program.cs`.
- Pipeline: `app.UseExceptionHandler();` (sem middleware customizado).

O handler recebe a exceção, decide o status/título/detalhe e delega a **serialização** ao **IProblemDetailsService**, garantindo que todas as respostas de erro sigam o formato RFC 7807.

---

## Mapeamento de Exceções

| Exceção | HTTP Status | Título | Uso |
|---------|-------------|--------|-----|
| **ValidationException** | 400 Bad Request | One or more validation errors occurred. | Falha nos validadores FluentValidation (request inválido). |
| **NotFoundException** | 404 Not Found | Not Found | Recurso não encontrado (ex.: usuário por id). |
| **UnauthorizedAccessException** | 401 Unauthorized | Unauthorized | Acesso não autorizado. |
| **ConflictException** | **422 Unprocessable Entity** | Conflict | Regra de negócio violada (ex.: e-mail duplicado). |
| **Demais** | 500 Internal Server Error | An unexpected error occurred. | Erros não mapeados. |

---

## Por que 422 para erros de negócio?

- **409 Conflict** é mais associado a conflito de versão (optimistic concurrency) ou recurso já existente em sentido “HTTP de recurso”.
- **422 Unprocessable Entity** indica que o servidor **entendeu** o pedido (sintaxe e tipo corretos), mas **não pode processá-lo** por restrições **semânticas/regras de negócio** (ex.: “este e-mail já está em uso”).

Assim, **ConflictException** (ex.: email duplicado) retorna **422** com `detail` contendo a mensagem de negócio (ex.: “The email is already in use.”), mantendo 400 para erros de **validação de entrada** e 409 reservado para outros tipos de conflito, se necessário no futuro.

---

## Segurança: StackTrace só em Development

O **GlobalExceptionHandler** adiciona o **StackTrace** ao `ProblemDetails` apenas quando `_environment.IsDevelopment()`:

- **Development**: `problemDetails.Extensions["stackTrace"] = exception.StackTrace` para facilitar debug.
- **Production**: nenhum stack trace na resposta; apenas título, status e detalhe controlados, evitando vazamento de informações sensíveis.

Logs de erro (ex.: `_logger.LogError`) continuam podendo registrar o stack trace no servidor, sem expô-lo ao cliente.
