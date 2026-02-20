using System.Diagnostics.CodeAnalysis;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.InMemory;

/// <summary>
/// No-op quando Kafka não está configurado; evita falha na aprovação do pedido.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NoOpOrderApprovedPublisher : IOrderApprovedPublisher
{
    public Task<bool> PublishOrderApprovedAsync(Order order, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
