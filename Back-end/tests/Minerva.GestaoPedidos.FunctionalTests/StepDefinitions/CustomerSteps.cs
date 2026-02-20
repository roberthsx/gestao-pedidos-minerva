using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.FunctionalTests.Hooks;
using Moq;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.StepDefinitions;

[Binding]
public class CustomerSteps
{
    private readonly ScenarioContext _scenarioContext;

    public CustomerSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"que sou um usuário autenticado")]
    public void GivenQueSouUmUsuarioAutenticado()
    {
        _scenarioContext["UseClientWithoutAuth"] = false;
    }

    [When(@"eu faço uma requisição GET para ""([^""]*)"" com token válido")]
    public async Task WhenEuFacoUmaRequisicaoGetParaComTokenValido(string endpoint)
    {
        var client = _scenarioContext.TryGetValue("CustomerFailureClient", out var fc) && fc is HttpClient failureClient
            ? failureClient
            : Hook.GetHttpClient();
        var response = await client.GetAsync(endpoint);
        _scenarioContext["Response"] = response;
    }

    [Then(@"o corpo da resposta deve ser uma lista \(possivelmente vazia\)")]
    public async Task ThenOCorpoDaRespostaDeveSerUmaLista()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
    }

    [Given(@"que o servidor está configurado para simular falha no lookup de clientes")]
    public void GivenQueOServidorEstaConfiguradoParaSimularFalhaNoLookupDeClientes()
    {
        var mockReadRepo = new Mock<ICustomerReadRepository>();
        mockReadRepo
            .Setup(q => q.GetLookupAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var factory = Hook.GetFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(ICustomerReadRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<ICustomerReadRepository>(_ => mockReadRepo.Object);
            });
        }).CreateClient();

        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        var token = AuthStepsHelper.GetAccessToken(Hook.GetHttpClient());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _scenarioContext["CustomerFailureClient"] = client;
    }

}

internal static class AuthStepsHelper
{
    public static string GetAccessToken(HttpClient client)
    {
        var response = client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" }).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("accessToken", out var accessToken))
            return accessToken.GetString()!;
        throw new InvalidOperationException("Resposta de login não contém data.accessToken (envelope ApiResponse).");
    }
}
