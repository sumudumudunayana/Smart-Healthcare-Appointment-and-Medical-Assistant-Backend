using System.Net;

namespace SmartHealthcare.API.Tests;

/// <summary>
/// Exercises a real route from every controller to ensure the application's
/// authorization pipeline protects endpoints before service code is invoked.
/// </summary>
public class ControllerAuthorizationCoverageTests : IClassFixture<HealthcareApiFactory>
{
    private readonly HttpClient _client;
    public ControllerAuthorizationCoverageTests(HealthcareApiFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("GET", "/api/AI/approvals/pending", 401)]
    [InlineData("POST", "/api/AI/test", 401)]
    [InlineData("POST", "/api/ai/process", 401)]
    [InlineData("GET", "/api/ai/recommendations/workflow/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/ai/scheduling/test", 401)]
    [InlineData("GET", "/api/ai/workflows", 401)]
    [InlineData("GET", "/api/Appointments/my", 401)]
    [InlineData("POST", "/api/Auth/logout", 415)]
    [InlineData("GET", "/api/Bills/my", 401)]
    [InlineData("GET", "/api/Departments", 401)]
    [InlineData("GET", "/api/DoctorSchedules/doctor/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("GET", "/api/Doctors", 401)]
    [InlineData("GET", "/api/InsuranceClaims/my", 401)]
    [InlineData("GET", "/api/InsurancePolicies/my", 401)]
    [InlineData("GET", "/api/LabReports/my", 401)]
    [InlineData("GET", "/api/MedicalRecords/my", 401)]
    [InlineData("GET", "/api/Notifications/my", 401)]
    [InlineData("GET", "/api/Payments/receptionist", 401)]
    [InlineData("GET", "/api/Prescriptions/my", 401)]
    [InlineData("GET", "/api/Receptionists", 401)]
    [InlineData("GET", "/api/Specializations", 401)]
    [InlineData("GET", "/api/Users/me", 401)]
    [InlineData("GET", "/api/AI/approvals/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/AI/approvals/11111111-1111-1111-1111-111111111111/approve", 401)]
    [InlineData("POST", "/api/AI/approvals/11111111-1111-1111-1111-111111111111/reject", 401)]
    [InlineData("GET", "/api/ai/workflows/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/Appointments", 401)]
    [InlineData("GET", "/api/Appointments/doctor/11111111-1111-1111-1111-111111111111/date/2026-10-11", 401)]
    [InlineData("GET", "/api/Appointments/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("PATCH", "/api/Appointments/11111111-1111-1111-1111-111111111111/cancel", 401)]
    [InlineData("GET", "/api/Appointments/doctor/my", 401)]
    [InlineData("GET", "/api/Appointments/admin", 401)]
    [InlineData("GET", "/api/Appointments/receptionist", 401)]
    [InlineData("POST", "/api/Bills", 401)]
    [InlineData("GET", "/api/Bills/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("GET", "/api/Bills/receptionist", 401)]
    [InlineData("POST", "/api/Departments", 401)]
    [InlineData("POST", "/api/DoctorSchedules", 401)]
    [InlineData("PUT", "/api/DoctorSchedules/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("DELETE", "/api/DoctorSchedules/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/Doctors", 401)]
    [InlineData("GET", "/api/Doctors/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/InsuranceClaims", 401)]
    [InlineData("GET", "/api/InsuranceClaims/bill/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/InsurancePolicies", 401)]
    [InlineData("GET", "/api/InsurancePolicies", 401)]
    [InlineData("GET", "/api/InsurancePolicies/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("POST", "/api/LabReports", 401)]
    [InlineData("GET", "/api/LabReports/record/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("GET", "/api/LabReports/doctor/my", 401)]
    [InlineData("POST", "/api/MedicalRecords", 401)]
    [InlineData("GET", "/api/MedicalRecords/appointment/11111111-1111-1111-1111-111111111111", 401)]
    [InlineData("GET", "/api/MedicalRecords/doctor/my", 401)]
    [InlineData("POST", "/api/Notifications", 401)]
    [InlineData("PATCH", "/api/Notifications/11111111-1111-1111-1111-111111111111/read", 401)]
    [InlineData("GET", "/api/Notifications/users", 401)]
    [InlineData("POST", "/api/Payments", 401)]
    [InlineData("GET", "/api/Payments/bill/11111111-1111-1111-1111-111111111111", 401)]
    public async Task ControllerEndpoint_WithoutCredentials_ReturnsExpectedPipelineStatus(
        string method,
        string path,
        int expectedStatus)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal((HttpStatusCode)expectedStatus, response.StatusCode);
    }
}
