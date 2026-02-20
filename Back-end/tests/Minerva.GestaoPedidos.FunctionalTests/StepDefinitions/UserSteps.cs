using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.FunctionalTests.Hooks;
using Moq;
using Reqnroll;

namespace Minerva.GestaoPedidos.FunctionalTests.StepDefinitions;

[Binding]
public class UserSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _httpClient;

    public UserSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _httpClient = Hook.GetHttpClient();
    }

    [Given(@"que eu tenho os dados de um usuário válido ""([^""]*)"" email ""([^""]*)""")]
    public void GivenQueEuTenhoOsDadosDeUmUsuarioValido(string firstName, string email)
    {
        var userData = new
        {
            FirstName = firstName,
            LastName = "Silva",
            Email = email,
            Active = true
        };
        _scenarioContext["UserData"] = userData;
    }

    [Given(@"que eu tenho os dados de um usuário com e-mail vazio")]
    public void GivenQueEuTenhoOsDadosDeUmUsuarioComEmailVazio()
    {
        _scenarioContext["UserData"] = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "",
            Active = true
        };
    }

    [Given(@"que o servidor está configurado para simular falha no repositório de usuários")]
    public void GivenQueOServidorEstaConfiguradoParaSimularFalhaNoRepositorioDeUsuarios()
    {
        var mockRepo = new Mock<IUserRepository>();
        mockRepo
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var factory = Hook.GetFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(IUserRepository));
                if (d != null) services.Remove(d);
                services.AddScoped<IUserRepository>(_ => mockRepo.Object);
            });
        }).CreateClient();

        if (client.BaseAddress != null)
            client.BaseAddress = new Uri(client.BaseAddress.ToString(), UriKind.Absolute);
        var token = AuthStepsHelper.GetAccessToken(Hook.GetHttpClient());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _scenarioContext["UserFailureClient"] = client;
    }

    [When(@"eu envio uma requisição POST para ""([^""]*)""")]
    public async Task WhenEuEnvioUmaRequisicaoPostPara(string endpoint)
    {
        var client = _scenarioContext.TryGetValue("UserFailureClient", out var fc) && fc is HttpClient failureClient
            ? failureClient
            : _httpClient;
        var userData = _scenarioContext.Get<object>("UserData");
        var response = await client.PostAsJsonAsync(endpoint, userData);
        _scenarioContext["Response"] = response;
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data))
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var userDto = System.Text.Json.JsonSerializer.Deserialize<UserDto>(data.GetRawText(), options);
                _scenarioContext["UserDto"] = userDto;
            }
        }
    }

    [When(@"eu envio uma requisição POST para ""([^""]*)"" com dados válidos")]
    public async Task WhenEuEnvioUmaRequisicaoPostParaComDadosValidos(string endpoint)
    {
        var client = _scenarioContext.Get<HttpClient>("UserFailureClient");
        var payload = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = $"u-{Guid.NewGuid():N}@m.local",
            Active = true
        };
        var response = await client.PostAsJsonAsync(endpoint, payload);
        _scenarioContext["Response"] = response;
    }

    [Then(@"o status code da resposta deve ser (\d+)")]
    public void ThenOStatusCodeDaRespostaDeveSer(int expectedStatusCode)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        response.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
    }

    [Then(@"o corpo da resposta deve conter o ID do usuário")]
    public void ThenOCorpoDaRespostaDeveConterOIdDoUsuario()
    {
        var userDto = _scenarioContext.Get<UserDto>("UserDto");
        userDto.Should().NotBeNull();
        userDto!.Id.Should().BeGreaterThan(0);
    }
}
