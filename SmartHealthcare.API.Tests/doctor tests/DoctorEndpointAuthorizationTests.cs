using System.Net;

namespace SmartHealthcare.API.Tests;

/// <summary>Security coverage for every doctor-facing controller action.</summary>
public class DoctorEndpointAuthorizationTests : IClassFixture<HealthcareApiFactory>
{
    private readonly HttpClient _client;

    public DoctorEndpointAuthorizationTests(HealthcareApiFactory factory) =>
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public static IEnumerable<object[]> ProtectedDoctorEndpoints
    {
        get
        {
            (string Method, string Path)[] actions =
            [
                ("GET", "/api/DoctorSchedules/doctor/11111111-1111-1111-1111-111111111111"),
                ("POST", "/api/DoctorSchedules"),
                ("PUT", "/api/DoctorSchedules/11111111-1111-1111-1111-111111111111"),
                ("DELETE", "/api/DoctorSchedules/11111111-1111-1111-1111-111111111111"),
                ("POST", "/api/LabReports"),
                ("GET", "/api/LabReports/record/11111111-1111-1111-1111-111111111111"),
                ("GET", "/api/LabReports/my"),
                ("GET", "/api/LabReports/doctor/my"),
                ("POST", "/api/MedicalRecords"),
                ("GET", "/api/MedicalRecords/appointment/11111111-1111-1111-1111-111111111111"),
                ("GET", "/api/MedicalRecords/doctor/my"),
                ("GET", "/api/MedicalRecords/my"),
                ("POST", "/api/Prescriptions"),
                ("GET", "/api/Prescriptions/record/11111111-1111-1111-1111-111111111111"),
                ("GET", "/api/Prescriptions/my"),
                ("GET", "/api/Prescriptions/doctor/my")
            ];
            string[] requestVariants = ["", "?source=portal", "?page=1", "?trace=true"];

            foreach ((string method, string path) in actions)
            foreach (string variant in requestVariants)
                yield return [method, path + variant];
        }
    }

    [Theory]
    [MemberData(nameof(ProtectedDoctorEndpoints))]
    public async Task DoctorFacingEndpoint_WithoutToken_ReturnsUnauthorized(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
