namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Thrown when a business rule conflict is detected (e.g. duplicate email). Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class ConflictException : BusinessException
{
    public ConflictException()
        : base("A conflict occurred with the current state of the resource.")
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}