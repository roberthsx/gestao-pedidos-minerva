namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Exceção de indisponibilidade de infraestrutura (ex.: banco de dados, timeout de conexão).
/// Retorna HTTP 503 Service Unavailable com corpo padronizado (type: InfrastructureError).
/// </summary>
public class InfrastructureException : ServiceUnavailableException
{
    /// <summary>Mensagem padrão para exibir ao usuário.</summary>
    public const string DefaultMessage = "O serviço de banco de dados está temporariamente indisponível. Tente novamente em instantes.";

    public InfrastructureException()
        : base(DefaultMessage)
    {
    }

    public InfrastructureException(string message)
        : base(message)
    {
    }

    public InfrastructureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}