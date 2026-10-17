namespace SmartHealthcare.API.Models;

public class Notification
{
    public Guid NotificationId { get; set; }

    // User who receives the notification
    public Guid UserId { get; set; }

    // User who created the notification
    public Guid? CreatedByUserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string Status { get; set; } = "Unread";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation property for the recipient
    public User? User { get; set; }
}