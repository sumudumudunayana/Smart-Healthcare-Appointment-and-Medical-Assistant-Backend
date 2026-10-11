using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

using Xunit;

namespace SmartHealthcare.API.Tests;

public class AuthIntegrationTests
    : IClassFixture<HealthcareApiFactory>
{
    private readonly HealthcareApiFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(HealthcareApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // IT03: Unknown email should return 401 Unauthorized.
    [Fact(DisplayName = "IT03 - Login with unknown email returns unauthorized")]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var request = new
        {
            Email = $"unknown-{Guid.NewGuid():N}@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Invalid email or password", body);
    }

    // IT04: Incorrect password should return 401 Unauthorized.
    [Fact(DisplayName = "IT04 - Login with incorrect password returns unauthorized")]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        string email =
            $"wrong-password-{Guid.NewGuid():N}@example.com";

        await SeedUserAsync(email, "CorrectPassword123!");

        var request = new
        {
            Email = email,
            Password = "IncorrectPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Invalid email or password", body);
    }

    // IT05: Successful login should issue a JWT accepted by the API.
    [Fact(DisplayName = "IT05 - Login token accesses administrator endpoint")]
    public async Task Login_WithValidCredentials_CanAccessProtectedEndpoint()
    {
        string email = $"admin-{Guid.NewGuid():N}@example.com";
        string password = "TestPassword123!";

        await SeedUserAsync(email, password);

        // Arrange: log in with valid credentials.
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        string loginBody =
            await loginResponse.Content.ReadAsStringAsync();

        Assert.True(
            loginResponse.StatusCode == HttpStatusCode.OK,
            $"Login failed. Status: {(int)loginResponse.StatusCode}. " +
            $"Response: {loginBody}");

        using var document = JsonDocument.Parse(loginBody);

        JsonElement root = document.RootElement;

        string token = root.GetProperty("token").GetString()
            ?? throw new InvalidOperationException(
                "The login response did not contain a JWT.");

        string refreshToken =
            root.GetProperty("refreshToken").GetString()
            ?? throw new InvalidOperationException(
                "The login response did not contain a refresh token.");

        Assert.NotEmpty(token);
        Assert.NotEmpty(refreshToken);

        // Inspect the JWT payload.
        // ReadJwtToken does not validate the signature.
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(token);

        var configuration = _factory.Services
            .GetRequiredService<IConfiguration>();

        string? expectedIssuer = configuration["Jwt:Issuer"];
        string? expectedAudience = configuration["Jwt:Audience"];

        Console.WriteLine();
        Console.WriteLine("========== JWT DIAGNOSTICS ==========");
        Console.WriteLine($"JWT issuer: {jwt.Issuer}");
        Console.WriteLine($"Expected issuer: {expectedIssuer}");
        Console.WriteLine(
            $"JWT audiences: {string.Join(", ", jwt.Audiences)}");
        Console.WriteLine($"Expected audience: {expectedAudience}");
        Console.WriteLine($"JWT algorithm: {jwt.Header.Alg}");
        Console.WriteLine(
            $"JWT expiration (UTC): {jwt.ValidTo:O}");

        Console.WriteLine(
            "JWT claims: " +
            string.Join(
                ", ",
                jwt.Claims.Select(
                    claim => $"{claim.Type}={claim.Value}")));

        Console.WriteLine("=====================================");
        Console.WriteLine();

        // Verify that the issued token contains the expected values.
        Assert.Equal(expectedIssuer, jwt.Issuer);

        Assert.NotNull(expectedAudience);
        Assert.Contains(expectedAudience, jwt.Audiences);

        Assert.Contains(
            jwt.Claims,
            claim =>
                (claim.Type == ClaimTypes.Role ||
                 claim.Type == "role" ||
                 claim.Type == "roles") &&
                claim.Value == "Administrator");

        // Call the protected endpoint with the issued token.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Specializations");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var protectedResponse = await _client.SendAsync(request);

        string protectedBody =
            await protectedResponse.Content.ReadAsStringAsync();

        Console.WriteLine("========== PROTECTED ENDPOINT ==========");
        Console.WriteLine(
            $"Status: {(int)protectedResponse.StatusCode} " +
            $"({protectedResponse.StatusCode})");
        Console.WriteLine($"Response: {protectedBody}");
        Console.WriteLine("========================================");

        Assert.True(
            protectedResponse.StatusCode == HttpStatusCode.OK,
            $"The JWT was issued, but the protected endpoint rejected it. " +
            $"Status: {(int)protectedResponse.StatusCode}. " +
            $"Response: {protectedBody}");
    }

    private async Task SeedUserAsync(
    string email,
    string password)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureCreatedAsync();

        var administratorRole = await db.Roles
            .FirstOrDefaultAsync(
                role => role.RoleName == "Administrator");

        Assert.NotNull(administratorRole);

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        string passwordHash =
            passwordService.HashPassword(password);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Integration Test Administrator",
            Email = email,
            PasswordHash = passwordHash,
            RoleId = administratorRole.RoleId,
            Role = administratorRole,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);

        await db.SaveChangesAsync();

        // Verify the saved user can be retrieved.
        var savedUser = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        Assert.NotNull(savedUser);

        Assert.Equal(email, savedUser.Email);
        Assert.Equal("Active", savedUser.Status);

        Assert.NotNull(savedUser.Role);

        Assert.Equal(
            "Administrator",
            savedUser.Role.RoleName);

        // Verify the generated password hash.
        Assert.True(
            passwordService.VerifyPassword(
                password,
                savedUser.PasswordHash),
            "The seeded user's password hash could not be verified.");

        Console.WriteLine(
            $"Seeded user verified: {savedUser.Email}");

        Console.WriteLine(
            $"Seeded role: {savedUser.Role.RoleName}");

        Console.WriteLine(
            $"Account status: {savedUser.Status}");

        Console.WriteLine(
            "Password hash verification: passed");
    }
}