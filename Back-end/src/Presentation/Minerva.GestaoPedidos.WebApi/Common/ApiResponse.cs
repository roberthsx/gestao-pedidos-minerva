using System.Text.Json.Serialization;

namespace Minerva.GestaoPedidos.WebApi.Common;

/// <summary>
/// Envelope padronizado de resposta da API (V1). Todas as respostas 2xx e erros retornam este formato.
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T? data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        Errors = null
    };

    public static ApiResponse<object> Failure(string message, IReadOnlyList<string>? errors = null) => new()
    {
        Success = false,
        Data = null,
        Message = message,
        Errors = errors?.ToList() ?? new List<string>()
    };
}
