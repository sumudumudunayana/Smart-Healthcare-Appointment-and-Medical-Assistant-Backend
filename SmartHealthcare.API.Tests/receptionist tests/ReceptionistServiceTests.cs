using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Receptionists;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class ReceptionistServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReceptionistService _receptionistService;
    private readonly PasswordService _passwordService = new();
    private readonly Guid _receptionistRoleId = Guid.NewGuid();

    public ReceptionistServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Roles.Add(new Role
        {
            RoleId = _receptionistRoleId,
            RoleName = "Receptionist"
        });
        _context.SaveChanges();

        _receptionistService = new ReceptionistService(
            _context,
            _passwordService);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_NormalizesAndHashesCredentials()
    {
        ReceptionistResponse response =
            await _receptionistService.CreateAsync(
                new CreateReceptionistRequest
                {
                    FullName = "  Alex Receptionist  ",
                    Email = "  ALEX@example.com ",
                    Phone = "  0771234567  ",
                    Password = "ReceptionistPassword123!"
                });

        User user = await _context.Users.SingleAsync();

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("Alex Receptionist", response.FullName);
        Assert.Equal("alex@example.com", response.Email);
        Assert.Equal("0771234567", response.Phone);
        Assert.Equal("Active", response.Status);
        Assert.Equal(_receptionistRoleId, user.RoleId);
        Assert.NotEqual("ReceptionistPassword123!", user.PasswordHash);
        Assert.True(
            _passwordService.VerifyPassword(
                "ReceptionistPassword123!",
                user.PasswordHash));
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmailIgnoringCase_ThrowsAndDoesNotAddUser()
    {
        var request = new CreateReceptionistRequest
        {
            FullName = "Alex Receptionist",
            Email = "alex@example.com",
            Password = "ReceptionistPassword123!"
        };

        await _receptionistService.CreateAsync(request);

        request.Email = "ALEX@EXAMPLE.COM";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _receptionistService.CreateAsync(request));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);
        Assert.Equal(1, await _context.Users.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_MissingReceptionistRole_ThrowsAndDoesNotAddUser()
    {
        _context.Roles.RemoveRange(_context.Roles);
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _receptionistService.CreateAsync(
                new CreateReceptionistRequest
                {
                    FullName = "Alex Receptionist",
                    Email = "alex@example.com",
                    Password = "ReceptionistPassword123!"
                }));

        Assert.Equal("Receptionist role was not found.", exception.Message);
        Assert.Empty(await _context.Users.ToListAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
