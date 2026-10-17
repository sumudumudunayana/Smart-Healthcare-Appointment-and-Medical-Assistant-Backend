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

    // ============================================================
    // CREATE NOTIFICATION
    // ============================================================

    public async Task<NotificationResponse> CreateAsync(
        Guid userId,
        CreateNotificationRequest request)
    {
        // Find the logged-in user who is creating the notification.
        User? currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (currentUser == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators and receptionists can create
        // notifications manually.
        Guid administratorRoleId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        Guid receptionistRoleId =
            Guid.Parse("33333333-3333-3333-3333-333333333333");

        if (currentUser.RoleId != administratorRoleId &&
            currentUser.RoleId != receptionistRoleId)
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can create notifications.");
        }

        // Validate the recipient.
        User? recipient = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId);

        if (recipient == null)
        {
            throw new ArgumentException(
                "Notification recipient was not found.");
        }

        // Validate the message.
        string message = request.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.");
        }

        if (message.Length > 2000)
        {
            throw new ArgumentException(
                "Notification message cannot exceed 2000 characters.");
        }

        // Validate the notification type.
        string notificationType =
            request.NotificationType?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException(
                "Notification type is required.");
        }

        if (notificationType.Length > 50)
        {
            throw new ArgumentException(
                "Notification type cannot exceed 50 characters.");
        }

        // Create the notification.
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),

            // The user who receives the notification.
            UserId = recipient.UserId,

            // The administrator or receptionist who created it.
            CreatedByUserId = currentUser.UserId,

            Message = message,
            NotificationType = notificationType,
            Status = "Unread",
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        return MapToResponse(notification);
    }

    // ============================================================
    // GET NOTIFICATIONS RECEIVED BY THE LOGGED-IN USER
    // ============================================================

    public async Task<List<NotificationResponse>> GetMyAsync(
        Guid userId)
    {
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

    // ============================================================
    // GET NOTIFICATIONS CREATED BY THE LOGGED-IN USER
    // ============================================================

    public async Task<List<NotificationResponse>> GetCreatedByMeAsync(
        Guid userId)
    {
        bool userExists = await _context.Users
            .AnyAsync(u => u.UserId == userId);

        if (!userExists)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.CreatedByUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,

                // Recipient's user ID.
                UserId = n.UserId,

                // Recipient details for the receptionist's screen.
                RecipientName = n.User != null
                    ? n.User.FullName
                    : "Unknown recipient",

                RecipientEmail = n.User != null
                    ? n.User.Email
                    : string.Empty,

                Message = n.Message,
                NotificationType = n.NotificationType,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();
    }

    // ============================================================
    // MARK NOTIFICATION AS READ
    // ============================================================

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

        // Only the recipient can mark the notification as read.
        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to modify this notification.");
        }

        // Preserve the original read timestamp.
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

    // ============================================================
    // GET USERS WHO CAN RECEIVE NOTIFICATIONS
    // ============================================================

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

    // ============================================================
    // MAP NOTIFICATION TO RESPONSE
    // ============================================================

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
}