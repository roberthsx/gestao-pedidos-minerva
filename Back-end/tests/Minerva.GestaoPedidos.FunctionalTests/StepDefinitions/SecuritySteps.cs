using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Minerva.GestaoPedidos.FunctionalTests.Hooks;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.StepDefinitions;

[Binding]
public class SecuritySteps
{
    private readonly ScenarioContext _scenarioContext;

    public SecuritySteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"que estou usando um cliente sem token de autenticação")]
    public void GivenQueEstouUsandoUmClienteSemTokenDeAutenticacao()
    {
        _scenarioContext["UseClientWithoutAuth"] = true;
    }

    [When(@"eu faço uma requisição GET para ""([^""]*)""")]
    public async Task WhenEuFacoUmaRequisicaoGetPara(string endpoint)
    {
        HttpClient client;
        if (_scenarioContext.TryGetValue("CustomerFailureClient", out var fc) && fc is HttpClient failureClient)
            client = failureClient;
        else if (_scenarioContext.TryGetValue("UseClientWithoutAuth", out var v) && v is true)
            client = Hook.GetHttpClientWithoutAuth();
        else
            client = Hook.GetHttpClient();
        var response = await client.GetAsync(endpoint);
        _scenarioContext["Response"] = response;
    }

    [When(@"eu faço uma requisição POST para ""([^""]*)"" sem token")]
    public async Task WhenEuFacoUmaRequisicaoPostParaSemToken(string endpoint)
    {
        var client = Hook.GetHttpClientWithoutAuth();
        var payload = new { FirstName = "Test", LastName = "User", Email = "test@minerva.com", Active = true };
        var response = await client.PostAsJsonAsync(endpoint, payload);
        _scenarioContext["Response"] = response;
    }

    [Then(@"a mensagem de erro deve indicar ""([^""]*)""")]
    public async Task ThenAMensagemDeErroDeveIndicar(string texto)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var json = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(json))
            json.Should().Contain(texto, "a API deve retornar mensagem em português");
    }
}
