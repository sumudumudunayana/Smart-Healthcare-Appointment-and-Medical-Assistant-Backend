using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Users;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly Guid _userId = Guid.NewGuid();

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var patientRole = new Role
        {
            RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            RoleName = "Patient"
        };
        _context.Roles.Add(patientRole);
        _context.Users.Add(new User
        {
            UserId = _userId,
            FullName = "Taylor Patient",
            Email = "taylor@example.com",
            PasswordHash = "test-hash",
            RoleId = patientRole.RoleId,
            Role = patientRole,
            Phone = "0770000000",
            Status = "Active"
        });
        _context.SaveChanges();

        _userService = new UserService(_context);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidRequest_TrimsNameAndClearsBlankPhone()
    {
        UserProfileResponse? response =
            await _userService.UpdateProfileAsync(
                _userId,
                new UpdateUserProfileRequest
                {
                    FullName = "  Taylor Patient  ",
                    Phone = "   "
                });

        Assert.NotNull(response);
        Assert.Equal(_userId, response.UserId);
        Assert.Equal("Taylor Patient", response.FullName);
        Assert.Null(response.Phone);

        User persistedUser = await _context.Users.SingleAsync();
        Assert.Equal("Taylor Patient", persistedUser.FullName);
        Assert.Null(persistedUser.Phone);
    }

    [Fact]
    public async Task UpdateProfileAsync_BlankName_ThrowsAndLeavesProfileUnchanged()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.UpdateProfileAsync(
                _userId,
                new UpdateUserProfileRequest
                {
                    FullName = "   ",
                    Phone = "0711111111"
                }));

        Assert.Equal("Full name is required.", exception.Message);

        User persistedUser = await _context.Users.SingleAsync();
        Assert.Equal("Taylor Patient", persistedUser.FullName);
        Assert.Equal("0770000000", persistedUser.Phone);
    }

    [Fact]
    public async Task UpdateProfileAsync_UnknownUser_ReturnsNull()
    {
        UserProfileResponse? response =
            await _userService.UpdateProfileAsync(
                Guid.NewGuid(),
                new UpdateUserProfileRequest
                {
                    FullName = "Taylor Patient"
                });

        Assert.Null(response);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
