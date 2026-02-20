using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.FunctionalTests.Hooks;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.StepDefinitions;

[Binding]
public class OrderSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _httpClient;

    public OrderSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _httpClient = Hook.GetHttpClient();
    }

    [Given(@"que eu realizei login com matrícula ""([^""]*)"" e senha ""([^""]*)""")]
    public async Task GivenQueEuRealizeiLoginComMatriculaESenha(string matricula, string senha)
    {
        var loginPayload = new { RegistrationNumber = matricula, Senha = senha };
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", loginPayload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            var root = doc.RootElement;
            root.TryGetProperty("data", out var data).Should().BeTrue("resposta deve ser envelope ApiResponse");
            data.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
            _scenarioContext["AccessToken"] = accessToken.GetString()!;
        }
    }

    [Given(@"existe um cliente e condição de pagamento no banco para o teste")]
    public async Task GivenExisteUmClienteECondicaoDePagamentoNoBancoParaOTeste()
    {
        var factory = Hook.GetFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = new Customer("Cliente E2E", "e2e@test.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var firstPayment = await db.PaymentConditions.OrderBy(p => p.Id).FirstAsync();
        _scenarioContext["CustomerId"] = customer.Id;
        _scenarioContext["PaymentConditionId"] = firstPayment.Id;
    }

    [When(@"eu crio um pedido com valor total maior que 5000")]
    public async Task WhenEuCrioUmPedidoComValorTotalMaiorQue5000()
    {
        await CreateOrderAsync(quantity: 26, unitPrice: 200m); // 5200 > 5000
    }

    [When(@"eu crio um pedido com valor total menor ou igual a 5000")]
    public async Task WhenEuCrioUmPedidoComValorTotalMenorOuIgualA5000()
    {
        await CreateOrderAsync(quantity: 25, unitPrice: 200m); // 5000
    }

    [When(@"eu aprovo o pedido criado")]
    public async Task WhenEuAprovoOPedidoCriado()
    {
        var orderId = _scenarioContext.Get<int>("CreatedOrderId");
        var token = _scenarioContext.Get<string>("AccessToken");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.PutAsync($"api/v1/Orders/{orderId}/approve", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data))
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var approved = System.Text.Json.JsonSerializer.Deserialize<OrderDto>(data.GetRawText(), options);
                _scenarioContext["LastOrder"] = approved;
            }
        }
    }

    [Then(@"o status do pedido deve ser ""([^""]*)""")]
    [When(@"o status do pedido deve ser ""([^""]*)""")]
    public void ThenOStatusDoPedidoDeveSer(string expectedStatus)
    {
        var order = _scenarioContext.Get<OrderDto>("LastOrder");
        order.Should().NotBeNull();
        order!.Status.Should().Be(expectedStatus);
    }

    [Then(@"o pedido deve ter RequiresManualApproval true")]
    public void ThenOPedidoDeveTerRequiresManualApprovalTrue()
    {
        var order = _scenarioContext.Get<OrderDto>("LastOrder");
        order.Should().NotBeNull();
        order!.RequiresManualApproval.Should().BeTrue();
    }

    [Then(@"o pedido deve ter RequiresManualApproval false")]
    public void ThenOPedidoDeveTerRequiresManualApprovalFalse()
    {
        var order = _scenarioContext.Get<OrderDto>("LastOrder");
        order.Should().NotBeNull();
        order!.RequiresManualApproval.Should().BeFalse();
    }

    [When(@"eu tento criar um pedido com CustomerId (\d+) e PaymentConditionId (\d+) e itens vazios")]
    public async Task WhenEuTentoCriarUmPedidoComCustomerIdEPaymentConditionIdEItensVazios(int customerId, int paymentConditionId)
    {
        var token = _scenarioContext.Get<string>("AccessToken");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var payload = new
        {
            CustomerId = customerId,
            PaymentConditionId = paymentConditionId,
            OrderDate = DateTime.UtcNow,
            Items = Array.Empty<object>()
        };
        var response = await _httpClient.PostAsJsonAsync("api/v1/Orders", payload);
        _scenarioContext["Response"] = response;
    }

    [When(@"eu tento criar um pedido com um item com quantidade zero")]
    public async Task WhenEuTentoCriarUmPedidoComUmItemComQuantidadeZero()
    {
        var customerId = _scenarioContext.Get<int>("CustomerId");
        var paymentConditionId = _scenarioContext.Get<int>("PaymentConditionId");
        var token = _scenarioContext.Get<string>("AccessToken");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var payload = new
        {
            CustomerId = customerId,
            PaymentConditionId = paymentConditionId,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "Produto Teste", Quantity = 0, UnitPrice = 10m } }
        };
        var response = await _httpClient.PostAsJsonAsync("api/v1/Orders", payload);
        _scenarioContext["Response"] = response;
    }

    [Then(@"a mensagem de erro deve ser ""([^""]*)""")]
    public async Task ThenAMensagemDeErroDeveSer(string mensagemEsperada)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(mensagemEsperada, "a API deve retornar a mensagem de validação em português");
    }

    [Then(@"o corpo da resposta deve conter mensagem de erro de validação")]
    public async Task ThenOCorpoDaRespostaDeveConterMensagemDeErroDeValidacao()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors", "a API retorna ProblemDetails com erros de validação");
    }

    private async Task CreateOrderAsync(int quantity, decimal unitPrice)
    {
        var customerId = _scenarioContext.Get<int>("CustomerId");
        var paymentConditionId = _scenarioContext.Get<int>("PaymentConditionId");
        var token = _scenarioContext.Get<string>("AccessToken");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            CustomerId = customerId,
            PaymentConditionId = paymentConditionId,
            OrderDate = DateTime.UtcNow,
            Items = new[] { new { ProductName = "Produto E2E", Quantity = quantity, UnitPrice = unitPrice } }
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/Orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            var root = doc.RootElement;
            root.TryGetProperty("data", out var data).Should().BeTrue("resposta deve ser envelope ApiResponse");
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var created = System.Text.Json.JsonSerializer.Deserialize<OrderDto>(data.GetRawText(), options);
            created.Should().NotBeNull();
            _scenarioContext["CreatedOrderId"] = created!.Id;
            _scenarioContext["LastOrder"] = created;
        }
    }
}
