using System.Net.Http.Json;
using System.Text.Json;
using Minerva.GestaoPedidos.WebApi.Common;

namespace Minerva.GestaoPedidos.IntegrationTests.Helpers;

/// <summary>
/// Lê respostas da API no formato envelope ApiResponse (V1).
/// </summary>
public static class ApiResponseExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<ApiResponse<T>?> ReadAsEnvelopeAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
    {
        return await content.ReadFromJsonAsync<ApiResponse<T>>(Options, cancellationToken);
    }

    /// <summary>Deserializa o envelope e retorna apenas a propriedade Data. Para tipos de referência, pode ser null.</summary>
    public static async Task<T?> ReadAsEnvelopeDataAsync<T>(this HttpContent content, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = await content.ReadAsEnvelopeAsync<T>(cancellationToken);
        return envelope?.Data;
    }
}
