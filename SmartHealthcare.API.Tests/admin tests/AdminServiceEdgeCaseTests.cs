using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Departments;
using SmartHealthcare.API.DTOs.Doctors;
using SmartHealthcare.API.DTOs.Specializations;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

/// <summary>Additional boundary, normalization, and filtering coverage for administrator-managed data.</summary>
public class AdminServiceEdgeCaseTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DepartmentService _departments;
    private readonly SpecializationService _specializations;
    private readonly DoctorService _doctors;
    private readonly Guid _doctorRoleId = Guid.NewGuid();
    private readonly Guid _cardiologyId = Guid.NewGuid();
    private readonly Guid _neurologyId = Guid.NewGuid();
    private readonly Guid _cardiologyDepartmentId = Guid.NewGuid();
    private readonly Guid _neurologyDepartmentId = Guid.NewGuid();

    public AdminServiceEdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new ApplicationDbContext(options);
        _context.Roles.Add(new Role { RoleId = _doctorRoleId, RoleName = "Doctor" });
        _context.Specializations.AddRange(
            new Specialization { SpecializationId = _cardiologyId, Name = "Cardiology" },
            new Specialization { SpecializationId = _neurologyId, Name = "Neurology" });
        _context.Departments.AddRange(
            new Department { DepartmentId = _cardiologyDepartmentId, DepartmentName = "Cardiology Unit" },
            new Department { DepartmentId = _neurologyDepartmentId, DepartmentName = "Neurology Unit" });
        _context.SaveChanges();
        _departments = new DepartmentService(_context);
        _specializations = new SpecializationService(_context);
        _doctors = new DoctorService(_context, new PasswordService());
    }

    [Fact]
    public async Task ADM01_Department_BlankName_IsRejected()
    {
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
            _departments.CreateAsync(new CreateDepartmentRequest { DepartmentName = "  " }));
        Assert.Equal("Department name is required.", error.Message);
    }

    [Fact]
    public async Task ADM02_Department_WhitespaceOptionalFields_AreStoredAsNull()
    {
        DepartmentResponse response = await _departments.CreateAsync(new CreateDepartmentRequest
        { DepartmentName = "Oncology", Description = " ", Location = "\t" });
        Assert.Null(response.Description); Assert.Null(response.Location);
    }

    [Fact]
    public async Task ADM03_Department_GetAll_EmptyDatabase_ReturnsEmptyList()
    {
        _context.Departments.RemoveRange(_context.Departments);
        await _context.SaveChangesAsync();
        Assert.Empty(await _departments.GetAllAsync());
    }

    [Fact]
    public async Task ADM04_Department_GetAll_MapsOptionalFields()
    {
        await _departments.CreateAsync(new CreateDepartmentRequest
        { DepartmentName = "Radiology", Description = "Imaging", Location = "B2" });
        DepartmentResponse result = Assert.Single(await _departments.GetAllAsync(),
            department => department.DepartmentName == "Radiology");
        Assert.Equal("Imaging", result.Description); Assert.Equal("B2", result.Location);
    }

    [Fact]
    public async Task ADM05_Specialization_Name_IsTrimmedBeforePersistence()
    {
        SpecializationResponse response = await _specializations.CreateAsync(
            new CreateSpecializationRequest { Name = "  Pediatrics  " });
        Assert.Equal("Pediatrics", response.Name);
    }

    [Fact]
    public async Task ADM06_Specialization_DuplicateNameIgnoringCase_IsRejected()
    {
        await _specializations.CreateAsync(new CreateSpecializationRequest { Name = "Oncology" });
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _specializations.CreateAsync(new CreateSpecializationRequest { Name = " ONCOLOGY " }));
        Assert.Equal("A specialization with this name already exists.", error.Message);
    }

    [Fact]
    public async Task ADM07_Specialization_GetAll_ReturnsOnlyPersistedRecords()
    {
        await _specializations.CreateAsync(new CreateSpecializationRequest { Name = "Dermatology" });
        List<SpecializationResponse> results = await _specializations.GetAllAsync();
        Assert.Contains(results, item => item.Name == "Dermatology");
        Assert.Equal(3, results.Count); // Two fixture records plus the newly created specialization.
    }

    [Fact]
    public async Task ADM08_Doctor_MissingEmail_IsRejected()
    {
        CreateDoctorRequest request = ValidDoctor(); request.Email = " ";
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _doctors.CreateAsync(request));
        Assert.Equal("Email is required.", error.Message);
    }

    [Fact]
    public async Task ADM09_Doctor_MissingPassword_IsRejected()
    {
        CreateDoctorRequest request = ValidDoctor(); request.Password = " ";
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _doctors.CreateAsync(request));
        Assert.Equal("Password is required.", error.Message);
    }

    [Fact]
    public async Task ADM10_Doctor_MissingLicenseNumber_IsRejected()
    {
        CreateDoctorRequest request = ValidDoctor(); request.LicenseNumber = " ";
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _doctors.CreateAsync(request));
        Assert.Equal("License number is required.", error.Message);
    }

    [Fact]
    public async Task ADM11_Doctor_MissingDoctorRole_IsRejected()
    {
        _context.Roles.RemoveRange(_context.Roles); await _context.SaveChangesAsync();
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => _doctors.CreateAsync(ValidDoctor()));
        Assert.Equal("Doctor role was not found.", error.Message);
    }

    [Fact]
    public async Task ADM12_Doctor_ZeroExperience_IsAllowed()
    {
        CreateDoctorRequest request = ValidDoctor(); request.Experience = 0;
        DoctorResponse response = await _doctors.CreateAsync(request);
        Assert.Equal(0, response.Experience);
    }

    [Fact]
    public async Task ADM13_Doctor_WithoutDepartment_IsCreatedWithNullDepartment()
    {
        CreateDoctorRequest request = ValidDoctor(); request.DepartmentId = null;
        DoctorResponse response = await _doctors.CreateAsync(request);
        Assert.Null(response.Department);
    }

    [Fact]
    public async Task ADM14_Doctor_NormalizesNameEmailLicenseAndPhone()
    {
        CreateDoctorRequest request = ValidDoctor();
        request.FullName = "  Dr. Ada Lovelace  "; request.Email = " ADA@EXAMPLE.COM ";
        request.LicenseNumber = "  SLMC-200  "; request.Phone = " 0770000000 ";
        DoctorResponse response = await _doctors.CreateAsync(request);
        Assert.Equal("Dr. Ada Lovelace", response.FullName); Assert.Equal("ada@example.com", response.Email);
        Assert.Equal("SLMC-200", response.LicenseNumber); Assert.Equal("0770000000", response.Phone);
    }

    [Fact]
    public async Task ADM15_Doctor_FilterBySpecialization_ReturnsMatchingDoctors()
    {
        await AddDoctorsAsync();
        List<DoctorResponse> results = await _doctors.GetAllAsync(null, _neurologyId, null);
        Assert.Single(results); Assert.Equal("Dr. Neuro", results[0].FullName);
    }

    [Fact]
    public async Task ADM16_Doctor_FilterByDepartment_ReturnsMatchingDoctors()
    {
        await AddDoctorsAsync();
        List<DoctorResponse> results = await _doctors.GetAllAsync(null, null, _cardiologyDepartmentId);
        Assert.Single(results); Assert.Equal("Dr. Cardio", results[0].FullName);
    }

    [Fact]
    public async Task ADM17_Doctor_CombinedFilters_ReturnOnlyIntersection()
    {
        await AddDoctorsAsync();
        List<DoctorResponse> results = await _doctors.GetAllAsync(null, _cardiologyId, _cardiologyDepartmentId);
        Assert.Single(results); Assert.Equal("Dr. Cardio", results[0].FullName);
    }

    [Fact]
    public async Task ADM18_Doctor_SearchBySpecialization_IsCaseInsensitive()
    {
        await AddDoctorsAsync();
        List<DoctorResponse> results = await _doctors.GetAllAsync("NEURO", null, null);
        Assert.Single(results); Assert.Equal("Dr. Neuro", results[0].FullName);
    }

    [Fact]
    public async Task ADM19_Doctor_SearchByDepartment_IsCaseInsensitive()
    {
        await AddDoctorsAsync();
        List<DoctorResponse> results = await _doctors.GetAllAsync("cardiology unit", null, null);
        Assert.Single(results); Assert.Equal("Dr. Cardio", results[0].FullName);
    }

    [Fact]
    public async Task ADM20_Doctor_WhitespaceSearch_DoesNotFilterResults()
    {
        await AddDoctorsAsync();
        Assert.Equal(2, (await _doctors.GetAllAsync("  ", null, null)).Count);
    }

    [Fact]
    public async Task ADM21_Doctor_UnmatchedFilter_ReturnsEmptyList()
    {
        await AddDoctorsAsync();
        Assert.Empty(await _doctors.GetAllAsync(null, Guid.NewGuid(), null));
    }

    private CreateDoctorRequest ValidDoctor() => new()
    {
        FullName = "Dr. Example",
        Email = $"{Guid.NewGuid()}@example.com",
        Phone = "0771234567",
        Password = "DoctorPassword123!",
        SpecializationId = _cardiologyId,
        DepartmentId = _cardiologyDepartmentId,
        LicenseNumber = $"SLMC-{Guid.NewGuid():N}",
        Experience = 5
    };

    private async Task AddDoctorsAsync()
    {
        CreateDoctorRequest cardio = ValidDoctor(); cardio.FullName = "Dr. Cardio"; cardio.LicenseNumber = "SLMC-CARDIO";
        CreateDoctorRequest neuro = ValidDoctor(); neuro.FullName = "Dr. Neuro"; neuro.SpecializationId = _neurologyId;
        neuro.DepartmentId = _neurologyDepartmentId; neuro.LicenseNumber = "SLMC-NEURO";
        await _doctors.CreateAsync(cardio); await _doctors.CreateAsync(neuro);
    }

    public void Dispose() => _context.Dispose();
}
