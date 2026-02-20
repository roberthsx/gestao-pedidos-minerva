# =============================================================================
# Minerva.GestaoPedidos - Exclusão de tópicos Kafka (Windows)
# =============================================================================
# Uso: ajuste $BootstrapServer ou defina $env:KAFKA_BOOTSTRAP_SERVERS
# Requer kafka-topics no PATH ou execute dentro do container Kafka.
# =============================================================================

$BootstrapServer = if ($env:KAFKA_BOOTSTRAP_SERVERS) { $env:KAFKA_BOOTSTRAP_SERVERS } else { "localhost:9092" }
$topics = @("orders.created")

foreach ($t in $topics) {
    Write-Host "Deleting topic: $t"
    & kafka-topics --bootstrap-server $BootstrapServer --delete --topic $t 2>$null
}
Write-Host "Kafka topics delete requested."
