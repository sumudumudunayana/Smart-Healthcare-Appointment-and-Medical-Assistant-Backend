using System.Net;

namespace SmartHealthcare.API.Tests;

/// <summary>Authorization coverage for all endpoints a patient can access.</summary>
public class PatientEndpointAuthorizationTests : IClassFixture<HealthcareApiFactory>
{
    private readonly HttpClient _client;

    public PatientEndpointAuthorizationTests(HealthcareApiFactory factory) =>
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public static IEnumerable<object[]> PatientEndpoints
    {
        get
        {
            const string id = "11111111-1111-1111-1111-111111111111";
            (string Method, string Path)[] actions =
            [
                ("POST", "/api/Appointments"),
                ("GET", "/api/Appointments/my"),
                ("GET", $"/api/Appointments/doctor/{id}/date/2026-10-12"),
                ("GET", $"/api/Appointments/{id}"),
                ("PATCH", $"/api/Appointments/{id}/cancel"),
                ("GET", "/api/InsurancePolicies/my"),
                ("GET", $"/api/InsurancePolicies/{id}"),
                ("GET", "/api/Users/me"),
                ("PUT", "/api/Users/me"),
                ("GET", "/api/LabReports/my"),
                ("GET", "/api/MedicalRecords/my"),
                ("GET", "/api/Prescriptions/my"),
                ("GET", "/api/DoctorSchedules/doctor/11111111-1111-1111-1111-111111111111"),
                ("GET", "/api/InsuranceClaims/my"),
                ("GET", "/api/Notifications/my"),
                ("PATCH", $"/api/Notifications/{id}/read"),
                ("GET", "/api/Doctors"),
                ("GET", $"/api/Doctors/{id}")
            ];
            string[] variants = ["", "?source=portal", "?page=1", "?trace=true"];

            foreach ((string method, string path) in actions)
            foreach (string variant in variants)
                yield return [method, path + variant];
        }
    }

    [Theory]
    [MemberData(nameof(PatientEndpoints))]
    public async Task PatientEndpoint_WithoutToken_ReturnsUnauthorized(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
