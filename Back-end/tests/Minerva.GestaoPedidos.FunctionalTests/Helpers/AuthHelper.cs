using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Minerva.GestaoPedidos.FunctionalTests.Helpers;

public static class AuthHelper
{
    private static readonly object Lock = new();

    /// <summary>
    /// Obtém um HttpClient com Bearer token válido (admin/Admin@123). Cada chamada usa um novo client do factory.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        var token = GetAccessToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Obtém um HttpClient sem token (para cenários 401).
    /// </summary>
    public static HttpClient CreateClientWithoutAuth(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        return client;
    }

    public static string GetAccessToken(HttpClient client)
    {
        var response = client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" }).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return data.GetProperty("accessToken").GetString()!;
    }
}