using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Application.Common.Constants;

/// <summary>
/// Mensagens centralizadas da aplicação (erros de negócio, logs). Valores em PT-BR para o usuário; nomes em inglês.
/// Use string.Format(Mensagem, arg0, ...) para mensagens com parâmetros.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApplicationMessages
{
    /// <summary>Mensagens relacionadas a clientes (Customer).</summary>
    public static class Customer
    {
        /// <summary>Cliente não encontrado. Parâmetros: customerId.</summary>
        public const string NotFound = "Cliente '{0}' não encontrado.";
    }

    /// <summary>Mensagens relacionadas a condições de pagamento (PaymentCondition).</summary>
    public static class PaymentCondition
    {
        /// <summary>Condição de pagamento não encontrada. Parâmetros: paymentConditionId.</summary>
        public const string NotFound = "Condição de pagamento '{0}' não encontrada.";
    }

    /// <summary>Mensagens relacionadas a pedidos (Order).</summary>
    public static class Order
    {
        /// <summary>Pedido não encontrado. Parâmetros: orderId.</summary>
        public const string NotFound = "Pedido '{0}' não encontrado.";

        /// <summary>Não é possível aprovar: pedido já está pago.</summary>
        public const string CannotApproveAlreadyPaid = "Não é possível aprovar o pedido: o pedido já está pago.";

        /// <summary>Não é possível aprovar: pedido cancelado.</summary>
        public const string CannotApproveCanceled = "Não é possível aprovar o pedido: o pedido está cancelado.";

        /// <summary>Não é possível aprovar: status inválido. Parâmetros: status.</summary>
        public const string CannotApproveInvalidStatus = "Não é possível aprovar o pedido: status inválido '{0}'.";

        /// <summary>Pedido não requer aprovação manual.</summary>
        public const string DoesNotRequireManualApproval = "O pedido não requer aprovação manual.";

        /// <summary>Log: idempotência bloqueou duplicata (check pré-insert). Parâmetros ILogger: CorrelationId, ExistingOrderId.</summary>
        public const string IdempotencyDuplicateBlockedPreInsert = "Idempotência: pedido duplicado bloqueado (check pré-insert). CorrelationId: {CorrelationId}. OrderId existente: {ExistingOrderId}.";

        /// <summary>Log: idempotência bloqueou duplicata (constraint DB). Parâmetros ILogger: CorrelationId, ExistingOrderId.</summary>
        public const string IdempotencyDuplicateBlocked = "Idempotência: pedido duplicado bloqueado. CorrelationId: {CorrelationId}. OrderId existente: {ExistingOrderId}.";
    }

    /// <summary>Mensagens relacionadas a autenticação (Auth).</summary>
    public static class Auth
    {
        // Reservado para mensagens de login/credenciais (ex.: AuthService, validators).
    }

    /// <summary>Mensagens relacionadas a publicação/consumo Kafka (logs e conciliação). Placeholders nomeados para ILogger.</summary>
    public static class Kafka
    {
        /// <summary>Sucesso: OrderCreated publicado. Parâmetro: OrderId.</summary>
        public const string PublishOrderCreatedSuccess = "OrderId={OrderId} publicado no Kafka (order-created). Worker criará DeliveryTerm assincronamente.";

        /// <summary>Falha ao publicar OrderCreated. Parâmetro: OrderId.</summary>
        public const string PublishOrderCreatedWarning = "Falha ao publicar no Kafka (order-created). OrderId={OrderId}. Conciliação futura pode identificar pelo log.";

        /// <summary>Erro ao publicar OrderCreated. Parâmetro: OrderId.</summary>
        public const string PublishOrderCreatedError = "Erro ao publicar no Kafka (order-created). OrderId={OrderId}. Conciliação futura pode identificar pelo log.";

        /// <summary>Falha ao publicar OrderApproved. Parâmetro: OrderId (para LogWarning/LogError).</summary>
        public const string PublishOrderApprovedWarning = "Falha ao publicar OrderApprovedEvent no Kafka. OrderId={OrderId}. Conciliação futura pode identificar pelo log.";

        /// <summary>Erro ao publicar OrderApproved. Parâmetro: OrderId (para LogError).</summary>
        public const string PublishOrderApprovedError = "Erro ao publicar OrderApprovedEvent no Kafka. OrderId={OrderId}. Conciliação futura pode identificar pelo log.";
    }
}
