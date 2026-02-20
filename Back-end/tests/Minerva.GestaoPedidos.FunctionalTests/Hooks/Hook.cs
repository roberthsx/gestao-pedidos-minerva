using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Minerva.GestaoPedidos.FunctionalTests.Infrastructure;
using Minerva.GestaoPedidos.WebApi;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.Hooks;

[Binding]
public class Hook
{
    private const string EnvAspNetCore = "ASPNETCORE_ENVIRONMENT";
    private const string EnvPostgres = "ConnectionStrings__Postgres";
    private const string EnvKafka = "Kafka__BootstrapServers";

    private static WebApplicationFactory<Program>? _factory;
    private static HttpClient? _httpClient;
    private static HttpClient? _httpClientNoAuth;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Environment.SetEnvironmentVariable(EnvAspNetCore, "Test");
        Environment.SetEnvironmentVariable(EnvPostgres, "Host=localhost;Database=test;Username=test;Password=test");
        Environment.SetEnvironmentVariable(EnvKafka, "");

        _factory = new CustomWebApplicationFactory();
        _httpClient = _factory.CreateClient();
        _httpClientNoAuth = _factory.CreateClient();
        if (_httpClient.BaseAddress != null)
            _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.ToString(), UriKind.Absolute);
        if (_httpClientNoAuth.BaseAddress != null)
            _httpClientNoAuth.BaseAddress = new Uri(_httpClientNoAuth.BaseAddress.ToString(), UriKind.Absolute);

        // Autentica uma vez para que requisições a endpoints protegidos tenham Bearer token (resposta em envelope ApiResponse)
        var loginResponse = _httpClient.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" }).GetAwaiter().GetResult();
        if (loginResponse.StatusCode == HttpStatusCode.OK)
        {
            var json = loginResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("accessToken", out var accessToken))
            {
                var token = accessToken.GetString();
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        _httpClientNoAuth?.Dispose();
        _httpClient?.Dispose();
        _factory?.Dispose();
        Environment.SetEnvironmentVariable(EnvAspNetCore, null);
        Environment.SetEnvironmentVariable(EnvPostgres, null);
        Environment.SetEnvironmentVariable(EnvKafka, null);
    }

    public static HttpClient GetHttpClient()
    {
        if (_httpClient == null)
            throw new InvalidOperationException("HttpClient not initialized. Make sure BeforeTestRun was called.");
        return _httpClient;
    }

    /// <summary>Cliente sem Authorization para cenários de segurança (401).</summary>
    public static HttpClient GetHttpClientWithoutAuth()
    {
        if (_httpClientNoAuth == null)
            throw new InvalidOperationException("HttpClient (no auth) not initialized. Make sure BeforeTestRun was called.");
        return _httpClientNoAuth;
    }

    public static WebApplicationFactory<Program> GetFactory()
    {
        if (_factory == null)
            throw new InvalidOperationException("WebApplicationFactory not initialized. Make sure BeforeTestRun was called.");
        return _factory;
    }
}
