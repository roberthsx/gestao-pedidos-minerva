using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.Contracts;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minerva.GestaoPedidos.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns_200_And_Token()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { RegistrationNumber = "admin", Senha = "Admin@123" };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("accessToken");
        json.Should().Contain("expiresIn");
        json.Should().Contain("user");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns_401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { RegistrationNumber = "admin", Senha = "WrongPassword123" };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("error");
    }

    [Fact]
    public async Task Login_WithWrongRegistrationNumber_Returns_401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { RegistrationNumber = "nonexistent", Senha = "Admin@123" };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMalformedPayload_MissingFields_Returns_400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("obrigat", "o validator retorna mensagens em PT (matrícula/senha obrigatória para o usuário)");
    }

    [Fact]
    public async Task Login_WithNullBody_Returns_400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("api/v1/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("obrigatório", "corpo da requisição é obrigatório");
    }

    [Fact]
    public async Task Endpoint_Returns_500_When_Infrastructure_Fails()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        mockAuth
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IAuthService));
                if (d != null) services.Remove(d);
                services.AddScoped<IAuthService>(_ => mockAuth.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });

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
}