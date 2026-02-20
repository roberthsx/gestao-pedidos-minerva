using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Worker.Resilience;
using Polly;

namespace Minerva.GestaoPedidos.Worker;

/// <summary>
/// HostedService que consome o tópico order-created (Kafka). Delega processamento ao <see cref="Application.Contracts.IOrderCreatedMessageHandler"/> e DLQ ao <see cref="Infrastructure.Messaging.Kafka.Abstractions.IKafkaProducerService"/>.
/// </summary>
public sealed class OrderCreatedKafkaConsumerHostedService : BackgroundService
{
    public const string OrderCreatedTopic = "order-created";
    public const string OrderCreatedDlqTopic = "order-created-dlq";
    public const string HeaderCorrelationId = "X-Correlation-ID";
    public const string HeaderCausationId = "X-Causation-ID";
    private static readonly TimeSpan CircuitBreakerDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReconnectDelayOnConsumeError = TimeSpan.FromSeconds(3);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderCreatedKafkaConsumerHostedService> _logger;

    public OrderCreatedKafkaConsumerHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrderCreatedKafkaConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers não configurado. Consumer não será iniciado.");
            return;
        }
        var groupId = _configuration["Kafka:ConsumerGroupId"]?.Trim() ?? "minerva-order-created-consumer";
        _logger.LogInformation("Kafka: {Topic}, GroupId: {GroupId}, DLQ: {DlqTopic}.", OrderCreatedTopic, groupId, OrderCreatedDlqTopic);
        await Task.Run(async () => await RunConsumerLoopAsync(bootstrapServers, groupId, stoppingToken).ConfigureAwait(false), stoppingToken).ConfigureAwait(false);
    }

    private async Task RunConsumerLoopAsync(string bootstrapServers, string groupId, CancellationToken stoppingToken)
    {
        var retryPolicy = WorkerConnectivityHelper.CreateOrderCreatedRetryPolicy(_logger);
        var builder = KafkaConsumerBuilderFactory.Create(bootstrapServers, groupId, _logger);
        var processor = new OrderCreatedConsumerMessageProcessor(_serviceProvider, _logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await WorkerConnectivityHelper.CanConnectAsync(_serviceProvider, _configuration, _logger, stoppingToken).ConfigureAwait(false))
                {
                    _logger.LogWarning("Circuit Breaker: Postgres ou Kafka indisponíveis. Aguardando {Seconds}s.", CircuitBreakerDelay.TotalSeconds);
                    await Task.Delay(CircuitBreakerDelay, stoppingToken).ConfigureAwait(false);
                    continue;
                }
                _logger.LogInformation("Conectado. Iniciando consumer no tópico {Topic}.", OrderCreatedTopic);
                using var consumer = builder.Build();
                consumer.Subscribe(OrderCreatedTopic);
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        await processor.ProcessOneMessageAsync(consumer, result, retryPolicy, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ConsumeException ex)
                    {
                        _logger.LogWarning(ex, "Erro ao consumir. Aguardando {Seconds}s.", ReconnectDelayOnConsumeError.TotalSeconds);
                        await Task.Delay(ReconnectDelayOnConsumeError, stoppingToken).ConfigureAwait(false);
                    }
                }
                consumer.Close();
                break;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no consumer. Aguardando {Seconds}s.", CircuitBreakerDelay.TotalSeconds);
                await Task.Delay(CircuitBreakerDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
