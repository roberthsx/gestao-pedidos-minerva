namespace Minerva.GestaoPedidos.Application.DTOs;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    bool Active);