using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Notifications;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationResponse> CreateAsync(
        Guid userId,
        CreateNotificationRequest request)
    {
        // Find the logged-in user
        User? currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (currentUser == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators and receptionists can manually
        // create notifications for users.
        if (currentUser.RoleId != Guid.Parse(
                "11111111-1111-1111-1111-111111111111") &&
            currentUser.RoleId != Guid.Parse(
                "33333333-3333-3333-3333-333333333333"))
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can create notifications.");
        }

        // Validate recipient
        User? recipient = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId);

        if (recipient == null)
        {
            throw new ArgumentException(
                "Notification recipient was not found.");
        }

        // Validate message
        string message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.");
        }

        // Validate notification type
        string notificationType =
            request.NotificationType.Trim();

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException(
                "Notification type is required.");
        }

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = recipient.UserId,
            Message = message,
            NotificationType = notificationType,
            Status = "Unread",
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        return MapToResponse(notification);
    }

    public async Task<List<NotificationResponse>> GetMyAsync(
        Guid userId)
    {
        // Make sure the logged-in user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.UserId == userId);

        if (!userExists)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Message = n.Message,
                NotificationType = n.NotificationType,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();
    }

    public async Task<NotificationResponse?> MarkAsReadAsync(
        Guid userId,
        Guid notificationId)
    {
        Notification? notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == notificationId);

        if (notification == null)
        {
            return null;
        }

        // A user can only mark their own notification as read
        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to modify this notification.");
        }

        // Avoid changing ReadAt repeatedly
        if (!string.Equals(
                notification.Status,
                "Read",
                StringComparison.OrdinalIgnoreCase))
        {
            notification.Status = "Read";
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return MapToResponse(notification);
    }

    private static NotificationResponse MapToResponse(
        Notification notification)
    {
        return new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            Message = notification.Message,
            NotificationType = notification.NotificationType,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }



    public async Task<List<object>> GetNotificationUsersAsync()
{
    return await _context.Users
        .AsNoTracking()
        .OrderBy(u => u.FullName)
        .Select(u => new
        {
            userId = u.UserId,
            fullName = u.FullName,
            email = u.Email,
            status = u.Status
        })
        .Cast<object>()
        .ToListAsync();
}
}