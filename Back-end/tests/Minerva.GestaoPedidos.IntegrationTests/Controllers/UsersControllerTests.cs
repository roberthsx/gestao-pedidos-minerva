using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.IntegrationTests.Helpers;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minerva.GestaoPedidos.IntegrationTests.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
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
        var response = await client.GetAsync("api/v1/Users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Returns_200_And_List()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("api/v1/Users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadAsEnvelopeDataAsync<List<UserDto>>();
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_WhenExists_Returns_200()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var listResponse = await client.GetAsync("api/v1/Users");
        listResponse.EnsureSuccessStatusCode();
        var users = await listResponse.Content.ReadAsEnvelopeDataAsync<List<UserDto>>();
        var firstId = users!.First().Id;

        // Act
        var response = await client.GetAsync($"api/v1/Users/{firstId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadAsEnvelopeDataAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(firstId);
    }

    [Fact]
    public async Task GetById_WhenNotExists_Returns_404()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        const int nonExistentId = 999999;

        // Act
        var response = await client.GetAsync($"api/v1/Users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidPayload_Returns_201()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = $"test-{unique}@minerva.local",
            Active = true
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Users", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadAsEnvelopeDataAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(payload.Email);
        user.FirstName.Should().Be("Test");
    }

    [Fact]
    public async Task GetAll_WhenEmpty_Returns_200_And_EmptyArray()
    {
        // Arrange
        var emptyRepo = new Mock<IUserReadRepository>();
        emptyRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserReadModel>());
        emptyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserReadModel?)null);

        var factory = new CustomWebApplicationFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IUserReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IUserReadRepository>(_ => emptyRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(client));

        // Act
        var response = await client.GetAsync("api/v1/Users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadAsEnvelopeDataAsync<List<UserDto>>();
        list.Should().NotBeNull();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Endpoint_Returns_500_When_Infrastructure_Fails()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo
            .Setup(x => x.AddAsync(It.IsAny<Minerva.GestaoPedidos.Domain.Entities.User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IUserRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IUserRepository>(_ => mockRepo.Object);
            });
        }).CreateClient();
        var token = await GetTokenAsync(_factory.CreateClient());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var payload = new { FirstName = "Test", LastName = "User", Email = $"test-{Guid.NewGuid():N}@minerva.local", Active = true };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Users", payload);

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