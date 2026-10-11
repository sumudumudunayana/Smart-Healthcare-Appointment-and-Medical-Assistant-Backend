using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Authentication;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AuthServiceTests : IDisposable
{
    private const string PatientRoleId =
        "44444444-4444-4444-4444-444444444444";

    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var patientRole = new Role
        {
            RoleId = Guid.Parse(PatientRoleId),
            RoleName = "Patient"
        };

        _context.Roles.Add(patientRole);
        _context.SaveChanges();

        var passwordService = new PasswordService();

        var configurationValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] =
                "test-key-for-authentication-tests-at-least-32-characters-long",
            ["Jwt:Issuer"] = "SmartHealthcare.Tests",
            ["Jwt:Audience"] = "SmartHealthcare.Tests",
            ["Jwt:ExpiresInMinutes"] = "60",
            ["Jwt:RefreshTokenExpiresInDays"] = "7"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        _authService = new AuthService(
            _context,
            passwordService,
            configuration);
    }

    // AUTH-01: Register with valid details
    [Fact(DisplayName = "AUTH-01 | Register with valid details")]
    public async Task RegisterAsync_ValidRequest_ReturnsAuthResponse()
    {
        var request = new RegisterRequest
        {
            FullName = "Test Patient",
            Email = "testpatient@example.com",
            Phone = "0771234567",
            Password = "TestPassword123!"
        };

        AuthResponse response = await _authService.RegisterAsync(request);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("Test Patient", response.FullName);
        Assert.Equal("testpatient@example.com", response.Email);
        Assert.Equal("Patient", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Equal(1, await _context.Patients.CountAsync());
        Assert.Equal(1, await _context.RefreshTokens.CountAsync());
    }

    // AUTH-02: Register with an existing email
    [Fact(DisplayName = "AUTH-02 | Register with a duplicate email")]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var request = new RegisterRequest
        {
            FullName = "Test Patient",
            Email = "duplicate@example.com",
            Phone = "0771234567",
            Password = "TestPassword123!"
        };

        await _authService.RegisterAsync(request);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        Assert.Equal(1, await _context.Users.CountAsync());
    }

    // AUTH-03: Login with valid credentials
    [Fact(DisplayName = "AUTH-03 | Login with valid credentials")]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        const string email = "login@example.com";
        const string password = "TestPassword123!";

        await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Login Patient",
            Email = email,
            Phone = "0771234567",
            Password = password
        });

        AuthResponse response = await _authService.LoginAsync(
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        Assert.NotNull(response);
        Assert.Equal(email, response.Email);
        Assert.Equal("Login Patient", response.FullName);
        Assert.Equal("Patient", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    // AUTH-04: Login with an incorrect password
    [Fact(DisplayName = "AUTH-04 | Login with an incorrect password")]
    public async Task LoginAsync_IncorrectPassword_ThrowsUnauthorizedAccessException()
    {
        const string email = "wrongpassword@example.com";

        await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Password Test",
            Email = email,
            Password = "CorrectPassword123!"
        });

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = "WrongPassword123!"
            }));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    // AUTH-05: Login with an unknown email
    [Fact(DisplayName = "AUTH-05 | Login with an unknown email")]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email = "unknown@example.com",
                Password = "TestPassword123!"
            }));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    // AUTH-06: Login with an inactive account
    [Fact(DisplayName = "AUTH-06 | Login with an inactive account")]
    public async Task LoginAsync_InactiveAccount_ThrowsUnauthorizedAccessException()
    {
        const string email = "inactive@example.com";
        const string password = "TestPassword123!";

        await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Inactive Patient",
            Email = email,
            Password = password
        });

        User user = await _context.Users
            .FirstAsync(u => u.Email == email);

        user.Status = "Inactive";
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            }));

        Assert.Equal("This account is not active.", exception.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}