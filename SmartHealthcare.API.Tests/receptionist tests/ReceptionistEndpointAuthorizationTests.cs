using System.Net;

namespace SmartHealthcare.API.Tests;

/// <summary>Authorization coverage for receptionist-facing API actions.</summary>
public class ReceptionistEndpointAuthorizationTests : IClassFixture<HealthcareApiFactory>
{
    private readonly HttpClient _client;

    public ReceptionistEndpointAuthorizationTests(HealthcareApiFactory factory) =>
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public static IEnumerable<object[]> ReceptionistEndpoints
    {
        get
        {
            const string id = "11111111-1111-1111-1111-111111111111";
            (string Method, string Path)[] actions =
            [
                ("POST", "/api/Receptionists"), ("GET", "/api/Receptionists"),
                ("POST", "/api/Bills"), ("GET", $"/api/Bills/{id}"),
                ("GET", "/api/Bills/my"), ("GET", "/api/Bills/receptionist"),
                ("POST", "/api/Payments"), ("GET", $"/api/Payments/bill/{id}"),
                ("GET", "/api/Payments/receptionist"), ("POST", "/api/InsuranceClaims"),
                ("GET", $"/api/InsuranceClaims/bill/{id}"), ("GET", "/api/InsuranceClaims/my"),
                ("POST", "/api/InsurancePolicies"), ("GET", "/api/InsurancePolicies"),
                ("GET", $"/api/InsurancePolicies/{id}"), ("POST", "/api/Notifications"),
                ("GET", "/api/Notifications/my"), ("PATCH", $"/api/Notifications/{id}/read"),
                ("GET", "/api/Notifications/users"), ("GET", "/api/Appointments/receptionist"),
                ("GET", $"/api/DoctorSchedules/doctor/{id}"), ("GET", "/api/Doctors"),
                ("GET", $"/api/Doctors/{id}")
            ];
            string[] variants = ["", "?source=desk", "?page=1", "?trace=true"];
            foreach ((string method, string path) in actions)
            foreach (string variant in variants)
                yield return [method, path + variant];
        }
    }

    [Theory]
    [MemberData(nameof(ReceptionistEndpoints))]
    public async Task ReceptionistEndpoint_WithoutToken_ReturnsUnauthorized(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
