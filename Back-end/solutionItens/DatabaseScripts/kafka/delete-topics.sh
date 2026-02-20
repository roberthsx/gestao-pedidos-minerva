#!/usr/bin/env bash
# =============================================================================
# Minerva.GestaoPedidos - Exclusão de tópicos Kafka
# =============================================================================
# Executar dentro do container Kafka ou com kafka-topics no PATH.
# Variável: KAFKA_BOOTSTRAP_SERVERS (padrão localhost:9092)
# =============================================================================

BOOTSTRAP_SERVERS="${KAFKA_BOOTSTRAP_SERVERS:-localhost:9092}"
KAFKA_TOPICS_CMD="${KAFKA_TOPICS_CMD:-kafka-topics}"

delete_topic() {
  echo "Deleting topic: $1"
  $KAFKA_TOPICS_CMD --bootstrap-server "$BOOTSTRAP_SERVERS" --delete --topic "$1" 2>/dev/null || true
}

delete_topic "orders.created"
echo "Kafka topics delete requested."
