using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.InsuranceClaims;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class InsuranceClaimServiceTests : IDisposable
{
    private static readonly Guid AdminRole = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ApplicationDbContext _context;
    private readonly InsuranceClaimService _service;
    private readonly User _admin;
    private readonly Bill _bill;
    private readonly InsurancePolicy _policy;

    public InsuranceClaimServiceTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _service = new InsuranceClaimService(_context);
        _admin = NewUser("Admin", AdminRole);
        User patientUser = NewUser("Patient", Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var patient = new Patient { PatientId = Guid.NewGuid(), UserId = patientUser.UserId };
        var appointment = new Appointment { AppointmentId = Guid.NewGuid(), PatientId = patient.PatientId, DoctorId = Guid.NewGuid(), ScheduleId = Guid.NewGuid(), AppointmentDate = new DateOnly(2026, 6, 10) };
        _bill = new Bill { BillId = Guid.NewGuid(), BillNumber = "B-1", PatientId = patient.PatientId, AppointmentId = appointment.AppointmentId, TotalAmount = 5000m, GeneratedDate = new DateOnly(2026, 6, 10) };
        _policy = new InsurancePolicy { InsurancePolicyId = Guid.NewGuid(), PatientId = patient.PatientId, ProviderName = "Care", PolicyNumber = "P-1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), CoverageAmount = 3000m, Status = "Active" };
        _context.AddRange(_admin, patientUser, patient, appointment, _bill, _policy);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesPendingClaim()
    {
        InsuranceClaimResponse result = await _service.CreateAsync(_admin.UserId, Request(2500m));
        Assert.Equal("Pending", result.Status); Assert.Equal(2500m, result.ClaimAmount);
        Assert.Equal("Details", result.ClaimDetails); Assert.Single(_context.InsuranceClaims);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_NonPositiveAmount_Throws(decimal amount)
    {
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(_admin.UserId, Request(amount)));
        Assert.Equal("Claim amount must be greater than zero.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_AmountAboveBill_Throws()
    {
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(_admin.UserId, Request(5001m)));
        Assert.Equal("Claim amount cannot exceed the bill amount.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_AmountAboveCoverage_Throws()
    {
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(_admin.UserId, Request(3001m)));
        Assert.Equal("Claim amount cannot exceed the insurance coverage amount.", error.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateClaim_Throws()
    {
        await _service.CreateAsync(_admin.UserId, Request(100m));
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(_admin.UserId, Request(100m)));
        Assert.Equal("An insurance claim already exists for this bill and policy.", error.Message);
    }

    private CreateInsuranceClaimRequest Request(decimal amount) => new()
    { BillId = _bill.BillId, InsurancePolicyId = _policy.InsurancePolicyId, ClaimAmount = amount, ClaimDetails = " Details " };

    private static User NewUser(string name, Guid role) => new()
    { UserId = Guid.NewGuid(), FullName = name, Email = $"{Guid.NewGuid()}@test.com", PasswordHash = "hash", RoleId = role, Status = "Active" };

    public void Dispose() => _context.Dispose();
}
