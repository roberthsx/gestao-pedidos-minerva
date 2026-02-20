using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.FunctionalTests.Hooks;
using Moq;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.StepDefinitions;

[Binding]
public class ResilienceSteps
{
    private readonly ScenarioContext _scenarioContext;

    public ResilienceSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"que o servidor está configurado para simular falha de infraestrutura no repositório de pedidos")]
    public void GivenQueOServidorEstaConfiguradoParaSimularFalhaDeInfraestrutura()
    {
        var mockRepo = new Mock<IOrderReadRepository>();
        mockRepo
            .Setup(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var factory = Hook.GetFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IOrderReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IOrderReadRepository>(_ => mockRepo.Object);
            });
        }).CreateClient();

        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);

        var token = ObterTokenDoClienteAutenticado();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _scenarioContext["FailureClient"] = client;
    }

    [When(@"eu faço uma requisição GET para listar pedidos com token válido")]
    public async Task WhenEuFacoUmaRequisicaoGetParaListarPedidosComTokenValido()
    {
        var client = _scenarioContext.Get<HttpClient>("FailureClient");
        var response = await client.GetAsync("api/v1/Orders?pageNumber=1&pageSize=10");
        _scenarioContext["Response"] = response;
    }

    [Then(@"a resposta deve ser ProblemDetails com status e título e não uma página de erro técnica")]
    public async Task ThenARespostaDeveSerProblemDetailsComStatusETitulo()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json", "a API retorna envelope ApiResponse em JSON");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("<!DOCTYPE", "não deve ser página HTML de erro");
        json.Should().NotContain("<html", "não deve ser página de erro técnica");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("success", out var success).Should().BeTrue("envelope ApiResponse deve ter 'success'");
        success.GetBoolean().Should().Be(false);
        root.TryGetProperty("message", out var message).Should().BeTrue("envelope ApiResponse deve ter 'message'");
        message.GetString().Should().NotBeNullOrEmpty();
    }

    [Then(@"o detalhe da resposta deve ser ""([^""]*)""")]
    public async Task ThenODetalheDaRespostaDeveSer(string detalheEsperado)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("message", out var message).Should().BeTrue("envelope ApiResponse deve ter 'message'");
        message.GetString().Should().Be(detalheEsperado, "a API deve retornar mensagem amigável em PT-BR");
    }

    private static string ObterTokenDoClienteAutenticado()
    {
        var client = Hook.GetHttpClient();
        var loginResponse = client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" }).GetAwaiter().GetResult();
        loginResponse.EnsureSuccessStatusCode();
        var json = loginResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("accessToken", out var accessToken))
            return accessToken.GetString()!;
        throw new InvalidOperationException("Resposta de login não contém data.accessToken (envelope ApiResponse).");
    }
}
