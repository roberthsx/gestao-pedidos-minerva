namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found (maps to HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException()
        : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}