using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Notifications;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class NotificationServiceTests : IDisposable
{
    private static readonly Guid AdministratorRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PatientRoleId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly ApplicationDbContext _context;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new NotificationService(_context);
    }

    [Fact]
    public async Task CreateAsync_Administrator_TrimsAndPersistsUnreadNotification()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        User recipient = await AddUserAsync(PatientRoleId, "Patient");

        NotificationResponse response = await _service.CreateAsync(administrator.UserId,
            new CreateNotificationRequest
            {
                UserId = recipient.UserId,
                Message = "  Your appointment is confirmed.  ",
                NotificationType = "  Appointment  "
            });

        Assert.Equal(recipient.UserId, response.UserId);
        Assert.Equal("Your appointment is confirmed.", response.Message);
        Assert.Equal("Appointment", response.NotificationType);
        Assert.Equal("Unread", response.Status);
        Assert.Null(response.ReadAt);
        Assert.Single(await _context.Notifications.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_NonAdministrativeUser_ThrowsAndDoesNotPersistNotification()
    {
        User patient = await AddUserAsync(PatientRoleId, "Patient");
        User recipient = await AddUserAsync(PatientRoleId, "Recipient");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(patient.UserId, new CreateNotificationRequest
            {
                UserId = recipient.UserId,
                Message = "Message",
                NotificationType = "General"
            }));

        Assert.Equal("Only administrators or receptionists can create notifications.", exception.Message);
        Assert.Empty(await _context.Notifications.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhitespaceMessage_ThrowsAndDoesNotPersistNotification()
    {
        User administrator = await AddUserAsync(AdministratorRoleId, "Administrator");
        User recipient = await AddUserAsync(PatientRoleId, "Patient");

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(administrator.UserId, new CreateNotificationRequest
            {
                UserId = recipient.UserId,
                Message = "   ",
                NotificationType = "General"
            }));

        Assert.Equal("Notification message is required.", exception.Message);
        Assert.Empty(await _context.Notifications.ToListAsync());
    }

    [Fact]
    public async Task GetMyAsync_ReturnsOnlyCurrentUsersNotificationsNewestFirst()
    {
        User patient = await AddUserAsync(PatientRoleId, "Patient");
        User otherPatient = await AddUserAsync(PatientRoleId, "Other Patient");
        DateTime now = DateTime.UtcNow;
        _context.Notifications.AddRange(
            NewNotification(patient.UserId, "Older", now.AddMinutes(-10)),
            NewNotification(patient.UserId, "Newest", now),
            NewNotification(otherPatient.UserId, "Private", now.AddMinutes(1)));
        await _context.SaveChangesAsync();

        List<NotificationResponse> notifications = await _service.GetMyAsync(patient.UserId);

        Assert.Collection(notifications,
            notification => Assert.Equal("Newest", notification.Message),
            notification => Assert.Equal("Older", notification.Message));
    }

    [Fact]
    public async Task GetMyAsync_UnknownUser_Throws()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetMyAsync(Guid.NewGuid()));

        Assert.Equal("User was not found.", exception.Message);
    }

    [Fact]
    public async Task MarkAsReadAsync_OtherUsersNotification_ThrowsAndLeavesItUnread()
    {
        User owner = await AddUserAsync(PatientRoleId, "Owner");
        User otherUser = await AddUserAsync(PatientRoleId, "Other User");
        Notification notification = NewNotification(owner.UserId, "Private", DateTime.UtcNow);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        UnauthorizedAccessException exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.MarkAsReadAsync(otherUser.UserId, notification.NotificationId));

        Assert.Equal("You do not have permission to modify this notification.", exception.Message);
        Assert.Equal("Unread", notification.Status);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyReadNotification_PreservesOriginalReadTime()
    {
        User owner = await AddUserAsync(PatientRoleId, "Owner");
        DateTime initialReadAt = DateTime.UtcNow.AddHours(-1);
        Notification notification = NewNotification(owner.UserId, "Read", DateTime.UtcNow.AddDays(-1));
        notification.Status = "Read";
        notification.ReadAt = initialReadAt;
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        NotificationResponse? response = await _service.MarkAsReadAsync(owner.UserId, notification.NotificationId);

        Assert.NotNull(response);
        Assert.Equal("Read", response.Status);
        Assert.Equal(initialReadAt, response.ReadAt);
        Assert.Equal(initialReadAt, notification.ReadAt);
    }

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

    private static Notification NewNotification(Guid userId, string message, DateTime createdAt) => new()
    {
        NotificationId = Guid.NewGuid(),
        UserId = userId,
        Message = message,
        NotificationType = "General",
        Status = "Unread",
        CreatedAt = createdAt
    };

    public void Dispose() => _context.Dispose();
}
