using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Minerva.GestaoPedidos.FunctionalTests.Helpers;

/// <summary>
/// Asserções padronizadas para resposta de erro (envelope ApiResponse com success=false).
/// </summary>
public static class ProblemDetailsAssertions
{
    public static async Task AssertProblemDetailsAsync(this HttpResponseMessage response, int expectedStatus = 500)
    {
        response.StatusCode.Should().Be((HttpStatusCode)expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("<!DOCTYPE").And.NotContain("<html");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().Be(false);
        root.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().NotBeNullOrEmpty();
    }
}
