using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.IntegrationTests.Helpers;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minerva.GestaoPedidos.IntegrationTests.Controllers;

public class PaymentConditionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentConditionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });
        loginResponse.EnsureSuccessStatusCode();
        var envelope = await loginResponse.Content.ReadAsEnvelopeAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope!.Data!.AccessToken);
        return client;
    }

    private sealed record LoginResult(string AccessToken, int ExpiresIn, object? User);

    [Fact]
    public async Task Request_WithoutToken_Returns_401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("api/v1/payment-conditions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLookup_WithToken_Returns_200_And_List()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("api/v1/payment-conditions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadAsEnvelopeDataAsync<List<PaymentConditionLookupDto>>();
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLookup_WhenEmpty_Returns_200_And_EmptyArray()
    {
        // Arrange
        var emptyReadRepo = new Mock<IPaymentConditionReadRepository>();
        emptyReadRepo.Setup(q => q.GetLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PaymentConditionReadModel>());

        var factory = new CustomWebApplicationFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IPaymentConditionReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IPaymentConditionReadRepository>(_ => emptyReadRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(client));

        // Act
        var response = await client.GetAsync("api/v1/payment-conditions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadAsEnvelopeDataAsync<List<PaymentConditionLookupDto>>();
        list.Should().NotBeNull();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Endpoint_Returns_500_When_Infrastructure_Fails()
    {
        // Arrange
        var mockReadRepo = new Mock<IPaymentConditionReadRepository>();
        mockReadRepo
            .Setup(q => q.GetLookupAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IPaymentConditionReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IPaymentConditionReadRepository>(_ => mockReadRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(client));

        // Act
        var response = await client.GetAsync("api/v1/payment-conditions");

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
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });
        loginResponse.EnsureSuccessStatusCode();
        var envelope = await loginResponse.Content.ReadAsEnvelopeAsync<LoginResult>();
        return envelope!.Data!.AccessToken;
    }
}