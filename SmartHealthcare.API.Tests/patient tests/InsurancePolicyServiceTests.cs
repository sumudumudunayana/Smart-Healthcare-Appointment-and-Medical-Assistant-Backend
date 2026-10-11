using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.InsurancePolicies;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class InsurancePolicyServiceTests : IDisposable
{
    private static readonly Guid AdministratorRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReceptionistRoleId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PatientRoleId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly ApplicationDbContext _context;
    private readonly InsurancePolicyService _service;

    public InsurancePolicyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new InsurancePolicyService(_context);
    }

    [Fact]
    public async Task CreateAsync_Receptionist_TrimsValuesAndCreatesActivePolicy()
    {
        User receptionist = await AddUserAsync(ReceptionistRoleId, "Receptionist");
        Patient patient = await AddPatientAsync("Taylor Patient");

        InsurancePolicyResponse response = await _service.CreateAsync(receptionist.UserId,
            new CreateInsurancePolicyRequest
            {
                PatientId = patient.PatientId,
                ProviderName = "  Health First  ",
                PolicyNumber = "  HF-100  ",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                CoverageAmount = 250000m
            });

        Assert.Equal("Health First", response.ProviderName);
        Assert.Equal("HF-100", response.PolicyNumber);
        Assert.Equal("Taylor Patient", response.PatientName);
        Assert.Equal("Active", response.Status);
        Assert.Single(await _context.InsurancePolicies.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_PatientUser_ThrowsAndDoesNotPersistPolicy()
    {
        Patient requester = await AddPatientAsync("Requester");
        Patient insuredPatient = await AddPatientAsync("Insured Patient");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(requester.UserId, ValidRequest(insuredPatient.PatientId)));

        Assert.Equal("Only administrators or receptionists can create insurance policies.", exception.Message);
        Assert.Empty(await _context.InsurancePolicies.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ThrowsAndDoesNotPersistPolicy()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        Patient patient = await AddPatientAsync("Patient");
        CreateInsurancePolicyRequest request = ValidRequest(patient.PatientId);
        request.StartDate = new DateOnly(2026, 6, 1);
        request.EndDate = new DateOnly(2026, 5, 31);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(administrator.UserId, request));

        Assert.Equal("Insurance policy end date cannot be before the start date.", exception.Message);
        Assert.Empty(await _context.InsurancePolicies.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicatePolicyNumber_ThrowsAndDoesNotAddSecondPolicy()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        Patient patient = await AddPatientAsync("Patient");
        await _service.CreateAsync(administrator.UserId, ValidRequest(patient.PatientId));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(administrator.UserId, ValidRequest(patient.PatientId)));

        Assert.Equal("An insurance policy with this policy number already exists.", exception.Message);
        Assert.Single(await _context.InsurancePolicies.ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_PatientCanReadOwnPolicy()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        Patient patient = await AddPatientAsync("Taylor Patient");
        InsurancePolicyResponse created = await _service.CreateAsync(administrator.UserId,
            ValidRequest(patient.PatientId));

        InsurancePolicyResponse? response = await _service.GetByIdAsync(
            patient.UserId, created.InsurancePolicyId);

        Assert.NotNull(response);
        Assert.Equal(created.InsurancePolicyId, response.InsurancePolicyId);
        Assert.Equal("Taylor Patient", response.PatientName);
    }

    [Fact]
    public async Task GetByIdAsync_OtherPatient_ThrowsUnauthorizedAccess()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        Patient policyOwner = await AddPatientAsync("Owner");
        Patient otherPatient = await AddPatientAsync("Other Patient");
        InsurancePolicyResponse created = await _service.CreateAsync(administrator.UserId,
            ValidRequest(policyOwner.PatientId));

        UnauthorizedAccessException exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetByIdAsync(otherPatient.UserId, created.InsurancePolicyId));

        Assert.Equal("You do not have permission to view this insurance policy.", exception.Message);
    }

    private static CreateInsurancePolicyRequest ValidRequest(Guid patientId) => new()
    {
        PatientId = patientId,
        ProviderName = "Health First",
        PolicyNumber = "HF-100",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        CoverageAmount = 250000m
    };

    private async Task<User> AddUserAsync(Guid roleId, string fullName)
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            RoleId = roleId,
            Status = "Active"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Patient> AddPatientAsync(string fullName)
    {
        User user = await AddUserAsync(PatientRoleId, fullName);
        var patient = new Patient { PatientId = Guid.NewGuid(), UserId = user.UserId };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public void Dispose() => _context.Dispose();
}
