using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Constants;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.IntegrationTests.Helpers;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Minerva.GestaoPedidos.IntegrationTests.Controllers;

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <param name="role">ApplicationRoles.Manager ou ApplicationRoles.Analyst - usa admin (Manager) para aprovação; ambiente em memória (TestServer) apenas.</param>
    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        var client = _factory.CreateClient();
        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        var loginPayload = new { RegistrationNumber = "admin", Senha = "Admin@123" };

        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", loginPayload);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginEnvelope = await loginResponse.Content.ReadAsEnvelopeAsync<LoginResult>();
        var loginContent = loginEnvelope!.Data!;
        loginContent.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginContent!.AccessToken);

        return client;
    }

    private sealed record LoginResult(string AccessToken, int ExpiresIn, LoginUser? User);

    private sealed record LoginUser(string Nome, string Perfil);

    /// <summary>Helper para obter Customer e PaymentCondition no banco em memória (seed do Program/Test).</summary>
    private async Task<(Customer Customer, PaymentCondition PaymentCondition)> EnsureCustomerAndPaymentConditionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync();
        var payment = await db.PaymentConditions.AsNoTracking().OrderBy(p => p.Id).FirstAsync();
        if (customer == null)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            customer = new Customer($"Minerva Foods {unique}", $"contato-{unique}@minervafoods.com");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }
        return (customer, payment);
    }

    [Fact]
    public async Task Request_WithoutToken_Returns_401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var getResponse = await client.GetAsync("api/v1/Orders?pageNumber=1&pageSize=10");
        var postResponse = await client.PostAsJsonAsync("api/v1/Orders", new { CustomerId = 1, PaymentConditionId = 1, OrderDate = DateTime.UtcNow, Items = new[] { new { ProductName = "P", Quantity = 1, UnitPrice = 10m } } });

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_And_Approve_Order_Greater_Than_5000_Should_Require_And_Then_Set_Paid_Status()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (customer, firstPayment) = await EnsureCustomerAndPaymentConditionAsync();
        var createOrderPayload = new
        {
            CustomerId = customer.Id,
            PaymentConditionId = firstPayment.Id,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "Produto A", Quantity = 10, UnitPrice = 600m } }
        };

        // Act
        var createResponse = await client.PostAsJsonAsync("api/v1/Orders", createOrderPayload);
        var createdOrder = await createResponse.Content.ReadAsEnvelopeDataAsync<OrderDto>();
        var approveResponse = await client.PutAsync($"api/v1/Orders/{createdOrder!.Id}/approve", null);
        var approvedOrder = await approveResponse.Content.ReadAsEnvelopeDataAsync<OrderDto>();

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdOrder.Should().NotBeNull();
        createdOrder!.TotalAmount.Should().Be(6000m);
        createdOrder.Status.Should().Be("Criado");
        createdOrder.RequiresManualApproval.Should().BeTrue();
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        approvedOrder.Should().NotBeNull();
        approvedOrder!.Status.Should().Be("Pago");
    }

    [Fact]
    public async Task Create_WithEmptyItems_Returns_400_And_ValidationErrors()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (customer, payment) = await EnsureCustomerAndPaymentConditionAsync();
        var payload = new
        {
            CustomerId = customer.Id,
            PaymentConditionId = payment.Id,
            OrderDate = DateTime.UtcNow,
            Items = Array.Empty<object>()
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().Be(false);
        doc.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
        json.Should().Contain("O pedido deve conter pelo menos um item.");
    }

    [Fact]
    public async Task Create_WithInvalidCustomerId_Returns_400_And_ValidationErrors()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (_, payment) = await EnsureCustomerAndPaymentConditionAsync();
        var payload = new
        {
            CustomerId = 0,
            PaymentConditionId = payment.Id,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "P", Quantity = 1, UnitPrice = 10m } }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetRawText().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Approve_WhenOrderDoesNotExist_Returns_404()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        const int nonExistentOrderId = 999999;

        // Act
        var response = await client.PutAsync($"api/v1/Orders/{nonExistentOrderId}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("999999");
        json.Should().Contain("encontrado", "a API retorna mensagens em PT-BR (ApplicationMessages); JSON pode vir com Unicode escapado");
    }

    [Fact]
    public async Task Create_TwiceWithSameBusinessKey_First_201_Second_409()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (customer, payment) = await EnsureCustomerAndPaymentConditionAsync();
        var orderDate = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        var payload = new
        {
            CustomerId = customer.Id,
            PaymentConditionId = payment.Id,
            OrderDate = orderDate,
            Items = new[] { new { ProductName = "Produto Idempotente", Quantity = 2, UnitPrice = 100m } }
        };

        // Act
        var first = await client.PostAsJsonAsync("api/v1/Orders", payload);
        var firstOrder = await first.Content.ReadAsEnvelopeDataAsync<OrderDto>();
        var second = await client.PostAsJsonAsync("api/v1/Orders", payload);
        var conflictJson = await second.Content.ReadAsStringAsync();

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        firstOrder.Should().NotBeNull();
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        conflictJson.Should().Contain("Pedido");
        conflictJson.Should().Contain("processado");
        conflictJson.Should().Contain("success");
        conflictJson.Should().Contain("false");
    }

    [Fact]
    public async Task Create_OrderUnder5000_Returns_201_WithStatusPago_And_NoManualApproval()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (customer, payment) = await EnsureCustomerAndPaymentConditionAsync();
        var payload = new
        {
            CustomerId = customer.Id,
            PaymentConditionId = payment.Id,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "Produto Barato", Quantity = 10, UnitPrice = 100m } }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadAsEnvelopeDataAsync<OrderDto>();
        created.Should().NotBeNull();
        created!.TotalAmount.Should().Be(1000m);
        created.Status.Should().Be("Pago");
        created.RequiresManualApproval.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaged_WithThreeOrders_Returns_200_WithItems_And_PaginationMetadata()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(ApplicationRoles.Manager);
        var (customer, payment) = await EnsureCustomerAndPaymentConditionAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orderDate = DateTime.UtcNow;
            for (int i = 0; i < 3; i++)
            {
                var order = Order.Create(customer.Id, payment.Id, orderDate, new[] { ($"Produto {i}", 1, 100m + i) });
                order.SetIdempotencyKey($"test-list-{Guid.NewGuid():N}");
                db.Orders.Add(order);
            }
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("api/v1/Orders?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadAsEnvelopeDataAsync<PagedResponse<OrderDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeNull();
        paged.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        paged.Items.Count.Should().BeGreaterThanOrEqualTo(3);
        paged.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPaged_WhenEmpty_Returns_200_And_EmptyArray()
    {
        // Arrange
        var emptyRepo = new Mock<IOrderReadRepository>();
        emptyRepo
            .Setup(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OrderReadModel>(), 0));

        var factory = new CustomWebApplicationFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IOrderReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IOrderReadRepository>(_ => emptyRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenForOrdersAsync(client));

        // Act
        var response = await client.GetAsync("api/v1/Orders?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadAsEnvelopeDataAsync<PagedResponse<OrderDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeNull().And.BeEmpty();
        paged.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOrder_WhenKafkaPublisherThrows_Returns_201_And_HandlesAsync()
    {
        // Arrange
        var throwingPublisher = new Mock<IOrderCreatedPublisher>();
        throwingPublisher
            .Setup(p => p.PublishOrderCreatedAsync(It.IsAny<Order>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka broker unavailable"));

        var factory = new CustomWebApplicationFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IOrderCreatedPublisher));
                if (d != null) services.Remove(d);
                services.AddSingleton<IOrderCreatedPublisher>(throwingPublisher.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenForOrdersAsync(client));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync();
            var payment = await db.PaymentConditions.AsNoTracking().OrderBy(p => p.Id).FirstAsync();
            if (customer == null)
            {
                var u = Guid.NewGuid().ToString("N")[..8];
                customer = new Customer($"Minerva {u}", $"c-{u}@m.com");
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            }
            var payload = new
            {
                CustomerId = customer.Id,
                PaymentConditionId = payment.Id,
                OrderDate = DateTime.UtcNow,
                Items = new[] { new { ProductName = "P", Quantity = 1, UnitPrice = 50m } }
            };

            // Act
            var response = await client.PostAsJsonAsync("api/v1/Orders", payload);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadAsEnvelopeDataAsync<OrderDto>();
            created.Should().NotBeNull();
            created!.Id.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Endpoint_Returns_500_When_Infrastructure_Fails()
    {
        // Arrange
        var mockRepo = new Mock<IOrderRepository>();
        mockRepo
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IOrderRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IOrderRepository>(_ => mockRepo.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenForOrdersAsync(client));
        var (customer, payment) = await EnsureCustomerAndPaymentConditionAsync();
        var payload = new
        {
            CustomerId = customer.Id,
            PaymentConditionId = payment.Id,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "P", Quantity = 1, UnitPrice = 10m } }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/Orders", payload);

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

    private static async Task<string> GetTokenForOrdersAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });
        loginResponse.EnsureSuccessStatusCode();
        var loginEnvelope = await loginResponse.Content.ReadAsEnvelopeAsync<LoginResult>();
        var login = loginEnvelope!.Data!;
        return login!.AccessToken;
    }
}