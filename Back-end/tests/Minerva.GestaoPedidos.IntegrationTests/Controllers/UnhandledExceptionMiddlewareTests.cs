using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using Minerva.GestaoPedidos.IntegrationTests.Helpers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minerva.GestaoPedidos.IntegrationTests.Controllers;

/// <summary>
/// Cenário de desastre (500): mock de infraestrutura que lança exceção.
/// Valida que a API retorna 500 e que o corpo segue o padrão da empresa sem expor StackTrace.
/// </summary>
public class UnhandledExceptionMiddlewareTests
{
    [Fact]
    public async Task WhenRepositoryThrows_Returns_500_And_ProblemDetailsJson_WithoutStackTrace()
    {
        // Arrange
        var mockRepo = new Mock<IOrderReadRepository>();
        mockRepo
            .Setup(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var factory = new CustomWebApplicationFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "Host=localhost;Database=test;Username=test;Password=test" }));
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOrderReadRepository));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped<IOrderReadRepository>(_ => mockRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(client));

        // Act
        var response = await client.GetAsync("api/v1/Orders?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().Be(false);
        root.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().NotBeNullOrEmpty();
        json.Should().NotContain("stackTrace");
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });
        loginResponse.EnsureSuccessStatusCode();
        var envelope = await loginResponse.Content.ReadAsEnvelopeAsync<LoginResult>();
        return envelope!.Data!.AccessToken;
    }

    private sealed record LoginResult(string AccessToken, int ExpiresIn, object? User);
}