using System.Diagnostics.CodeAnalysis;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.InMemory;

/// <summary>
/// No-op quando Kafka não está configurado; evita falha na criação do pedido.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NoOpOrderCreatedPublisher : IOrderCreatedPublisher
{
    public Task<bool> PublishOrderCreatedAsync(Order order, string? correlationId, string? causationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
