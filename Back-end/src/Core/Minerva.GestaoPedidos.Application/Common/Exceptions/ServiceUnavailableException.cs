namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an infrastructure dependency is unavailable (e.g. database down).
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException()
        : base("Serviço temporariamente indisponível. Tente novamente em instantes.")
    {
    }

    public ServiceUnavailableException(string message)
        : base(message)
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}