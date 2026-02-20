namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Exception for invalid requests (e.g. aprovar pedido já pago) — maps to HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}