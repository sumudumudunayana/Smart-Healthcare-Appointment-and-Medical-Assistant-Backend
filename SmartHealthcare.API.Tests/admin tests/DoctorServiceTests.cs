using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Doctors;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class DoctorServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DoctorService _doctorService;
    private readonly PasswordService _passwordService;

    private readonly Guid _doctorRoleId =
        Guid.NewGuid();

    private readonly Guid _specializationId =
        Guid.NewGuid();

    private readonly Guid _departmentId =
        Guid.NewGuid();

    public DoctorServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _passwordService = new PasswordService();

        _context.Roles.Add(new Role
        {
            RoleId = _doctorRoleId,
            RoleName = "Doctor"
        });

        _context.Specializations.Add(new Specialization
        {
            SpecializationId = _specializationId,
            Name = "Cardiology"
        });

        _context.Departments.Add(new Department
        {
            DepartmentId = _departmentId,
            DepartmentName = "Cardiology Department"
        });

        _context.SaveChanges();

        _doctorService = new DoctorService(
            _context,
            _passwordService);
    }

    private CreateDoctorRequest CreateValidRequest(
        string fullName = "Dr. John Smith",
        string email = "john@example.com",
        string licenseNumber = "SLMC-10001")
    {
        return new CreateDoctorRequest
        {
            FullName = fullName,
            Email = email,
            Phone = "0771234567",
            Password = "DoctorPassword123!",
            SpecializationId = _specializationId,
            DepartmentId = _departmentId,
            LicenseNumber = licenseNumber,
            Experience = 5
        };
    }

    // DOC-01: Create a doctor with valid details
    [Fact(DisplayName = "DOC-01 | Create doctor with valid details")]
    public async Task CreateAsync_ValidRequest_CreatesDoctorSuccessfully()
    {
        CreateDoctorRequest request = CreateValidRequest();

        DoctorResponse response =
            await _doctorService.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, response.DoctorId);
        Assert.NotEqual(Guid.Empty, response.UserId);

        Assert.Equal("Dr. John Smith", response.FullName);
        Assert.Equal("john@example.com", response.Email);
        Assert.Equal("Cardiology", response.Specialization);
        Assert.Equal("Cardiology Department", response.Department);
        Assert.Equal("SLMC-10001", response.LicenseNumber);
        Assert.Equal(5, response.Experience);
        Assert.Equal("Active", response.Status);

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Equal(1, await _context.Doctors.CountAsync());
    }

    // DOC-02: Reject a duplicate email
    [Fact(DisplayName = "DOC-02 | Reject duplicate doctor email")]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _doctorService.CreateAsync(
            CreateValidRequest());

        CreateDoctorRequest duplicateRequest =
            CreateValidRequest(
                fullName: "Dr. Jane Smith",
                email: "JOHN@example.com",
                licenseNumber: "SLMC-10002");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _doctorService.CreateAsync(duplicateRequest));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Equal(1, await _context.Doctors.CountAsync());
    }

    // DOC-03: Reject a duplicate license number
    [Fact(DisplayName = "DOC-03 | Reject duplicate license number")]
    public async Task CreateAsync_DuplicateLicense_ThrowsInvalidOperationException()
    {
        await _doctorService.CreateAsync(
            CreateValidRequest());

        CreateDoctorRequest duplicateRequest =
            CreateValidRequest(
                fullName: "Dr. Jane Smith",
                email: "jane@example.com",
                licenseNumber: "slmc-10001");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _doctorService.CreateAsync(duplicateRequest));

        Assert.Equal(
            "A doctor with this license number already exists.",
            exception.Message);

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Equal(1, await _context.Doctors.CountAsync());
    }

    // DOC-04: Reject an invalid specialization
    [Fact(DisplayName = "DOC-04 | Reject nonexistent specialization")]
    public async Task CreateAsync_InvalidSpecialization_ThrowsArgumentException()
    {
        CreateDoctorRequest request = CreateValidRequest();

        request.SpecializationId = Guid.NewGuid();

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _doctorService.CreateAsync(request));

        Assert.Equal(
            "The selected specialization was not found.",
            exception.Message);

        Assert.Empty(await _context.Users.ToListAsync());
        Assert.Empty(await _context.Doctors.ToListAsync());
    }

    // DOC-05: Reject an invalid department
    [Fact(DisplayName = "DOC-05 | Reject nonexistent department")]
    public async Task CreateAsync_InvalidDepartment_ThrowsArgumentException()
    {
        CreateDoctorRequest request = CreateValidRequest();

        request.DepartmentId = Guid.NewGuid();

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _doctorService.CreateAsync(request));

        Assert.Equal(
            "The selected department was not found.",
            exception.Message);

        Assert.Empty(await _context.Users.ToListAsync());
        Assert.Empty(await _context.Doctors.ToListAsync());
    }

    // DOC-06: Reject negative experience
    [Fact(DisplayName = "DOC-06 | Reject negative experience")]
    public async Task CreateAsync_NegativeExperience_ThrowsArgumentException()
    {
        CreateDoctorRequest request = CreateValidRequest();

        request.Experience = -1;

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _doctorService.CreateAsync(request));

        Assert.Equal(
            "Experience cannot be negative.",
            exception.Message);

        Assert.Empty(await _context.Users.ToListAsync());
        Assert.Empty(await _context.Doctors.ToListAsync());
    }

    // DOC-07: Reject a blank full name
    [Fact(DisplayName = "DOC-07 | Reject blank doctor name")]
    public async Task CreateAsync_BlankFullName_ThrowsArgumentException()
    {
        CreateDoctorRequest request = CreateValidRequest();

        request.FullName = "   ";

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _doctorService.CreateAsync(request));

        Assert.Equal(
            "Full name is required.",
            exception.Message);

        Assert.Empty(await _context.Users.ToListAsync());
    }

    // DOC-08: Retrieve all doctors
    [Fact(DisplayName = "DOC-08 | Retrieve all doctors alphabetically")]
    public async Task GetAllAsync_ExistingDoctors_ReturnsAlphabeticalList()
    {
        await _doctorService.CreateAsync(
            CreateValidRequest(
                fullName: "Dr. Sarah Wilson",
                email: "sarah@example.com",
                licenseNumber: "SLMC-10003"));

        await _doctorService.CreateAsync(
            CreateValidRequest(
                fullName: "Dr. Alice Brown",
                email: "alice@example.com",
                licenseNumber: "SLMC-10004"));

        List<DoctorResponse> result =
            await _doctorService.GetAllAsync(
                search: null,
                specializationId: null,
                departmentId: null);

        Assert.Equal(2, result.Count);

        Assert.Equal("Dr. Alice Brown", result[0].FullName);
        Assert.Equal("Dr. Sarah Wilson", result[1].FullName);
    }

    // DOC-09: Search doctors by name
    [Fact(DisplayName = "DOC-09 | Search doctors by name")]
    public async Task GetAllAsync_SearchByName_ReturnsMatchingDoctors()
    {
        await _doctorService.CreateAsync(
            CreateValidRequest(
                fullName: "Dr. John Smith",
                email: "john@example.com",
                licenseNumber: "SLMC-10005"));

        await _doctorService.CreateAsync(
            CreateValidRequest(
                fullName: "Dr. Sarah Wilson",
                email: "sarah@example.com",
                licenseNumber: "SLMC-10006"));

        List<DoctorResponse> result =
            await _doctorService.GetAllAsync(
                search: "john",
                specializationId: null,
                departmentId: null);

        Assert.Single(result);
        Assert.Equal("Dr. John Smith", result[0].FullName);
    }

    // DOC-10: Retrieve a doctor by ID
    [Fact(DisplayName = "DOC-10 | Retrieve doctor by ID")]
    public async Task GetByIdAsync_ExistingDoctor_ReturnsDoctor()
    {
        DoctorResponse createdDoctor =
            await _doctorService.CreateAsync(
                CreateValidRequest());

        DoctorResponse? result =
            await _doctorService.GetByIdAsync(
                createdDoctor.DoctorId);

        Assert.NotNull(result);
        Assert.Equal(
            createdDoctor.DoctorId,
            result.DoctorId);

        Assert.Equal(
            createdDoctor.FullName,
            result.FullName);

        Assert.Equal(
            createdDoctor.Email,
            result.Email);
    }

    // DOC-11: Return null for a nonexistent doctor
    [Fact(DisplayName = "DOC-11 | Return null for nonexistent doctor")]
    public async Task GetByIdAsync_NonexistentDoctor_ReturnsNull()
    {
        DoctorResponse? result =
            await _doctorService.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}