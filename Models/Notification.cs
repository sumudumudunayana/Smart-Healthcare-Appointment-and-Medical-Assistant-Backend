namespace SmartHealthcare.API.Models;

public class Notification
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string Status { get; set; } = "Unread";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public User? User { get; set; }
}