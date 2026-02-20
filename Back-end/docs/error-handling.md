# Tratamento de Erros

Este documento descreve o uso do **GlobalExceptionHandlerMiddleware**, do envelope **ApiResponse** e o mapeamento de exceções para códigos HTTP no **Minerva Gestão de Pedidos**.

---

## Envelope ApiResponse

A API utiliza um **envelope padronizado** para respostas de sucesso e de erro (não RFC 7807 Problem Details):

```json
{
  "success": true,
  "data": { ... },
  "message": null,
  "errors": null
}
```

Em erro:

```json
{
  "success": false,
  "data": null,
  "message": "Um ou mais erros de validação ocorreram.",
  "errors": ["'Customer Id' must be greater than '0'."]
}
```

Benefícios: **contrato estável** para clientes, respostas 2xx e 4xx/5xx no mesmo formato e **extensibilidade** (ex.: em Development, `errors` pode incluir stack trace para 500).

---

## GlobalExceptionHandlerMiddleware

Em vez de um `IExceptionHandler` (.NET 8+), o projeto usa um **middleware** customizado:

- **GlobalExceptionHandlerMiddleware** (`WebApi/Middleware/GlobalExceptionHandlerMiddleware.cs`) envolve o pipeline e captura exceções não tratadas.
- Registro: o middleware é adicionado ao pipeline em `Program.cs` com `app.UseMiddleware<GlobalExceptionHandlerMiddleware>()`.
- Fluxo: exceção → log (com CorrelationId) → construção do corpo ApiResponse (status, message, errors) → escrita na resposta HTTP.

O middleware também trata exceções de **idempotência** (OrderAlreadyExistsException) com log em nível Warning, sem stack trace no corpo.

---

## Mapeamento de Exceções

| Exceção / Situação        | HTTP Status | Uso |
|---------------------------|-------------|-----|
| **ValidationException**   | 400 Bad Request | Falha nos validadores FluentValidation (request inválido). |
| **BadRequestException**   | 400 Bad Request | Requisição inválida (mensagem customizada). |
| **NotFoundException**     | 404 Not Found | Recurso não encontrado (ex.: pedido, usuário, cliente). |
| **UnauthorizedAccessException** | 401 Unauthorized | Acesso não autorizado. |
| **ConflictException**     | **422 Unprocessable Entity** | Regra de negócio violada (ex.: e-mail duplicado, pedido já pago). |
| **BusinessException**     | **422 Unprocessable Entity** | Outras violações de regra de negócio. |
| **OrderAlreadyExistsException** | **409 Conflict** | Pedido já processado (idempotência / concorrência). |
| **InfrastructureException** / **ServiceUnavailableException** | **503 Service Unavailable** | Falha de infraestrutura (ex.: banco indisponível). |
| **Demais**                | 500 Internal Server Error | Erros não mapeados. |

---

## Por que 422 para erros de negócio?

- **409 Conflict** é reservado para conflito de recurso (ex.: idempotência, duplicata de pedido) — **OrderAlreadyExistsException** retorna 409.
- **422 Unprocessable Entity** indica que o servidor **entendeu** o pedido (sintaxe e tipo corretos), mas **não pode processá-lo** por restrições **semânticas/regras de negócio** (ex.: “este e-mail já está em uso”, “pedido já está pago”).

Assim, **ConflictException** e **BusinessException** retornam **422** com `message` e `errors` contendo a mensagem de negócio; 400 fica para **validação de entrada** e 409 para **conflito de recurso/idempotência**.

---

## Segurança: StackTrace só em não-Produção

O **GlobalExceptionHandlerMiddleware** adiciona o **StackTrace** ao array `errors` apenas quando **não está em Production** e o status é 500:

- **Development / outros**: `errors` pode incluir o stack trace para facilitar debug.
- **Production**: nenhum stack trace na resposta; apenas `message` e `errors` controlados, evitando vazamento de informações sensíveis.

Logs de erro (ex.: `_logger.LogError`) continuam podendo registrar o stack trace no servidor, sem expô-lo ao cliente.

---

## Filtro de envelope para respostas 2xx/4xx dos controllers

O **ApiResponseEnvelopeFilter** encapsula respostas dos controllers no envelope ApiResponse:

- **OkObjectResult** e **CreatedAtActionResult**: valor envolvido em `ApiResponse.Ok(value)`.
- **BadRequestObjectResult** e **UnauthorizedObjectResult**: convertidos em `ApiResponse.Failure(...)` com status 400 ou 401.

Assim, tanto sucesso quanto erros retornados pelos controllers (antes de chegar ao middleware) seguem o mesmo contrato.
