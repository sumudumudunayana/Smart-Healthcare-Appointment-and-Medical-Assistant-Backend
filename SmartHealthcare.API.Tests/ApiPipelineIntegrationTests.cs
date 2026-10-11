using System.Net;

namespace SmartHealthcare.API.Tests;

public class ApiPipelineIntegrationTests
    : IClassFixture<HealthcareApiFactory>
{
    private readonly HttpClient _client;

    public ApiPipelineIntegrationTests(HealthcareApiFactory factory)
    {
        _client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    // IT01: Verify that the API request pipeline enforces authorization.
    [Fact(DisplayName = "IT01 - Specializations endpoint rejects unauthenticated requests")]
    public async Task GetSpecializations_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Specializations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // IT02: Verify that an unknown API route returns 404.
    [Fact(DisplayName = "IT02 - Unknown API route returns not found")]
    public async Task GetUnknownRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/this-route-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}