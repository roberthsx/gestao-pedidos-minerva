using System.Net;
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
public class AuthSteps
{
    private readonly ScenarioContext _scenarioContext;

    public AuthSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("que sou um usuário autenticável com matrícula \"(.*)\" e senha \"(.*)\"")]
    public void GivenQueSouUmUsuarioAutenticavelComMatriculaESenha(string matricula, string senha)
    {
        _scenarioContext["LoginRegistrationNumber"] = matricula;
        _scenarioContext["LoginSenha"] = senha;
    }

    [Given("que sou um usuário com matrícula \"(.*)\" e senha incorreta \"(.*)\"")]
    public void GivenQueSouUmUsuarioComMatriculaESenhaIncorreta(string matricula, string senha)
    {
        _scenarioContext["LoginRegistrationNumber"] = matricula;
        _scenarioContext["LoginSenha"] = senha;
    }

    [Given("que eu envio um payload de login vazio")]
    public void GivenQueEuEnvioUmPayloadDeLoginVazio()
    {
        _scenarioContext["LoginPayloadVazio"] = true;
    }

    [Given("que o servidor está configurado para simular falha no serviço de autenticação")]
    public void GivenQueOServidorEstaConfiguradoParaSimularFalhaNoServicoDeAutenticacao()
    {
        var mockAuth = new Mock<IAuthService>();
        mockAuth
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var factory = Hook.GetFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IAuthService));
                if (d != null) services.Remove(d);
                services.AddScoped<IAuthService>(_ => mockAuth.Object);
            });
        }).CreateClient();

        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        _scenarioContext["AuthFailureClient"] = client;
    }

    [When("eu realizo login na API")]
    public async Task WhenEuRealizoLoginNaAPI()
    {
        var client = Hook.GetHttpClient();
        if (_scenarioContext.TryGetValue("AuthFailureClient", out var failureClient) && failureClient is HttpClient failClient)
            client = failClient;

        HttpResponseMessage response;
        if (_scenarioContext.TryGetValue("LoginPayloadVazio", out var vazio) && vazio is true)
            response = await client.PostAsJsonAsync("api/v1/auth/login", new { });
        else if (_scenarioContext.TryGetValue("LoginRegistrationNumber", out var mat) && mat is string matricula &&
                 _scenarioContext.TryGetValue("LoginSenha", out var sen) && sen is string senha)
            response = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = matricula, Senha = senha });
        else
            response = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = "admin", Senha = "Admin@123" });

        _scenarioContext["Response"] = response;
    }

    [When("eu realizo login na API com matrícula \"(.*)\" e senha \"(.*)\"")]
    public async Task WhenEuRealizoLoginNaAPIComMatriculaESenha(string matricula, string senha)
    {
        var client = _scenarioContext.Get<HttpClient>("AuthFailureClient");
        var response = await client.PostAsJsonAsync("api/v1/auth/login", new { RegistrationNumber = matricula, Senha = senha });
        _scenarioContext["Response"] = response;
    }

    [Then("o corpo da resposta deve conter \"(.*)\", \"(.*)\" e \"(.*)\"")]
    public async Task ThenOCorpoDaRespostaDeveConter(string key1, string key2, string key3)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(key1).And.Contain(key2).And.Contain(key3);
    }

    [Then("a resposta deve indicar erro de autenticação")]
    public async Task ThenARespostaDeveIndicarErroDeAutenticacao()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("error");
    }

    [Then("a mensagem de erro deve conter \"(.*)\"")]
    public async Task ThenAMensagemDeErroDeveConter(string texto)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(texto, "mensagem de erro da API em PT-BR");
    }
}
