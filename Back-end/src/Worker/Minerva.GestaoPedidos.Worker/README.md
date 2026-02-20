# Minerva.GestaoPedidos.Worker

Serviço de processamento assíncrono que consome eventos do Kafka e persiste dados no PostgreSQL.

## Descrição

O Worker roda em processo separado da API e é responsável por:

- **order-created**: consumir o tópico `order-created` (Kafka), criar o registro em `DeliveryTerms` (10 dias) no PostgreSQL e fazer commit do offset. Em falha após 3 retries, envia a mensagem para a DLQ `order-created-dlq`.

Padrão oficial de tópicos do projeto: **hífens** (ex.: `order-created`, `order-created-dlq`, `order-approved`).

## Fluxo de Dados

1. A API cria o pedido no PostgreSQL e publica no tópico **order-created** (Kafka).
2. O **OrderCreatedKafkaConsumerHostedService** consome `order-created`, valida o payload (OrderId), verifica idempotência e insere em `DeliveryTerms` com 10 dias de prazo.
3. Em falha persistente (ex.: broker indisponível), a mensagem é enviada para **order-created-dlq** para conciliação.

## Resiliência

- **Retry (Polly)**: até 3 tentativas por mensagem antes de enviar para a DLQ.
- **Circuit Breaker**: em falha de infraestrutura (Postgres/Kafka), o consumer aguarda antes de reconectar (evita tight loop).
- **Idempotência**: se já existir `DeliveryTerm` para o mesmo OrderId, a mensagem é ignorada e o offset é commitado.

## Health Check

| Endpoint            | Uso       | Descrição                                                    |
|---------------------|-----------|--------------------------------------------------------------|
| `GET /health/live`  | Liveness  | Processo vivo. Não depende de Postgres ou Kafka.            |
| `GET /health`       | Readiness | Estado de Postgres e Kafka (metadata do broker).             |

## Configuração

Variáveis de ambiente / `appsettings.json`:

- **ConnectionStrings:Postgres** – obrigatório; mesmo banco da API.
- **Kafka:BootstrapServers** – obrigatório para consumo e para publicação na DLQ.
- **Kafka:ConsumerGroupId** – opcional; padrão `minerva-order-created-consumer`.
- **Kafka:ConsumerSeekToEarliestOnStart** – opcional; `true` para reprocessar desde o início ao atribuir partições.

O Docker Compose cria os tópicos `order-created` e `order-created-dlq` na subida do ambiente.
