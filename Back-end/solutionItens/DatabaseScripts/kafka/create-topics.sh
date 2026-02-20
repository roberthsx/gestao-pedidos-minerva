#!/usr/bin/env bash
# =============================================================================
# Minerva.GestaoPedidos - Criação de tópicos Kafka
# =============================================================================
# Executar dentro do container Kafka ou com kafka-topics no PATH:
#   docker exec -it <container_kafka> bash /path/to/create-topics.sh
# Ou com Kafka local: ./create-topics.sh (BootstrapServers padrão localhost:9092)
# =============================================================================

BOOTSTRAP_SERVERS="${KAFKA_BOOTSTRAP_SERVERS:-localhost:9092}"
KAFKA_TOPICS_CMD="${KAFKA_TOPICS_CMD:-kafka-topics}"

create_topic() {
  local topic=$1
  local partitions=${2:-3}
  local replication=${3:-1}
  echo "Creating topic: $topic (partitions=$partitions, replication=$replication)"
  $KAFKA_TOPICS_CMD --bootstrap-server "$BOOTSTRAP_SERVERS" \
    --create \
    --topic "$topic" \
    --partitions "$partitions" \
    --replication-factor "$replication" \
    --if-not-exists 2>/dev/null || true
}

# Tópico para eventos de pedidos criados (OrderSyncWorker consome)
create_topic "orders.created" 3 1

# Opcional: tópico para outros eventos de domínio no futuro
# create_topic "users.created" 3 1

echo "Kafka topics ready."
