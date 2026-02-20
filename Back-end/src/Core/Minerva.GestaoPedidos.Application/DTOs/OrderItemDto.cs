namespace Minerva.GestaoPedidos.Application.DTOs;

public record OrderItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
