# =============================================================================
# Minerva.GestaoPedidos - Criação de tópicos Kafka (Windows / Docker)
# =============================================================================
# Uso: docker exec -it <container_kafka> kafka-topics --bootstrap-server localhost:9092 --create --topic orders.created --partitions 3 --replication-factor 1 --if-not-exists
# Ou execute este script após ajustar $BootstrapServer (ex.: kafka:9092 dentro da rede Docker).
# =============================================================================

$BootstrapServer = if ($env:KAFKA_BOOTSTRAP_SERVERS) { $env:KAFKA_BOOTSTRAP_SERVERS } else { "localhost:9092" }

$topics = @(
    @{ Name = "orders.created"; Partitions = 3; ReplicationFactor = 1 },
    @{ Name = "orders.approved"; Partitions = 3; ReplicationFactor = 1 }
)

foreach ($t in $topics) {
    Write-Host "Creating topic: $($t.Name)"
    & kafka-topics --bootstrap-server $BootstrapServer `
        --create `
        --topic $t.Name `
        --partitions $t.Partitions `
        --replication-factor $t.ReplicationFactor `
        --if-not-exists 2>$null
}

Write-Host "Kafka topics ready."
