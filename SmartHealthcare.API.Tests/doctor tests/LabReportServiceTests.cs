using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.LabReports;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class LabReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LabReportService _service;
    private readonly User _doctorUser;
    private readonly Patient _patient;
    private readonly MedicalRecord _record;

    public LabReportServiceTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _service = new LabReportService(_context);
        _doctorUser = User("Doctor", "doctor@test.com");
        User patientUser = User("Patient", "patient@test.com");
        var doctor = new Doctor { DoctorId = Guid.NewGuid(), UserId = _doctorUser.UserId, SpecializationId = Guid.NewGuid(), LicenseNumber = "L-1" };
        _patient = new Patient { PatientId = Guid.NewGuid(), UserId = patientUser.UserId };
        _record = new MedicalRecord { RecordId = Guid.NewGuid(), DoctorId = doctor.DoctorId, PatientId = _patient.PatientId, AppointmentId = Guid.NewGuid() };
        _context.AddRange(_doctorUser, patientUser, doctor, _patient, _record);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_TrimsAndPersistsReport()
    {
        LabReportResponse result = await _service.CreateAsync(_doctorUser.UserId, Request());
        Assert.Equal("Blood Count", result.ReportName); Assert.Equal("Pathology", result.ReportType);
        Assert.Equal("/files/report.pdf", result.FilePath); Assert.Single(_context.LabReports);
    }

    [Fact]
    public async Task CreateAsync_BlankReportName_Throws()
    {
        CreateLabReportRequest request = Request(); request.ReportName = "  ";
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(_doctorUser.UserId, request));
        Assert.Equal("Report name is required.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_UnknownDoctor_Throws()
    {
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(Guid.NewGuid(), Request()));
        Assert.Equal("Doctor profile was not found.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_OtherDoctorsRecord_Throws()
    {
        User otherUser = User("Other", "other@test.com");
        _context.AddRange(otherUser, new Doctor { DoctorId = Guid.NewGuid(), UserId = otherUser.UserId, SpecializationId = Guid.NewGuid(), LicenseNumber = "L-2" });
        await _context.SaveChangesAsync();
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(otherUser.UserId, Request()));
        Assert.Equal("You can only add lab reports to your own medical records.", error.Message);
    }

    [Fact]
    public async Task GetMyAsync_ReturnsPatientsReportsNewestFirst()
    {
        await _service.CreateAsync(_doctorUser.UserId, Request("Older"));
        await _service.CreateAsync(_doctorUser.UserId, Request("Newer"));
        List<LabReportResponse> reports = await _service.GetMyAsync(_patient.UserId);
        Assert.Equal(2, reports.Count); Assert.Contains(reports, report => report.ReportName == "Older");
        Assert.Contains(reports, report => report.ReportName == "Newer");
    }

    [Fact]
    public async Task CreateAsync_WhitespaceOptionalValues_AreStoredAsNull()
    {
        CreateLabReportRequest request = Request(); request.ReportType = " "; request.FilePath = " ";
        LabReportResponse report = await _service.CreateAsync(_doctorUser.UserId, request);
        Assert.Null(report.ReportType); Assert.Null(report.FilePath);
    }

    [Fact]
    public async Task GetByRecordAsync_UnknownDoctor_Throws()
    {
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetByRecordAsync(Guid.NewGuid(), _record.RecordId));
        Assert.Equal("Doctor profile was not found.", error.Message);
    }

    [Fact]
    public async Task GetMyAsync_UnknownPatient_Throws()
    {
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetMyAsync(Guid.NewGuid()));
        Assert.Equal("Patient profile was not found.", error.Message);
    }

    [Fact]
    public async Task GetMyDoctorReportsAsync_ReturnsOnlyAuthenticatedDoctorsReports()
    {
        await _service.CreateAsync(_doctorUser.UserId, Request());
        User otherUser = User("Other", "other2@test.com");
        var otherDoctor = new Doctor { DoctorId = Guid.NewGuid(), UserId = otherUser.UserId, SpecializationId = Guid.NewGuid(), LicenseNumber = "L-3" };
        _context.AddRange(otherUser, otherDoctor);
        await _context.SaveChangesAsync();
        Assert.Single(await _service.GetMyDoctorReportsAsync(_doctorUser.UserId));
        Assert.Empty(await _service.GetMyDoctorReportsAsync(otherUser.UserId));
    }

    private CreateLabReportRequest Request(string name = "  Blood Count  ") => new()
    { RecordId = _record.RecordId, ReportName = name, ReportType = " Pathology ", FilePath = " /files/report.pdf " };

    private static User User(string name, string email) => new()
    { UserId = Guid.NewGuid(), FullName = name, Email = email, PasswordHash = "hash", RoleId = Guid.NewGuid(), Status = "Active" };

    public void Dispose() => _context.Dispose();
}
