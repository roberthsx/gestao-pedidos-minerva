namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Base exception for business rule violations (e.g. 422 Unprocessable Entity, 409 Conflict).
/// </summary>
public class BusinessException : Exception
{
    public BusinessException()
    {
    }

    public BusinessException(string message)
        : base(message)
    {
    }

    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}