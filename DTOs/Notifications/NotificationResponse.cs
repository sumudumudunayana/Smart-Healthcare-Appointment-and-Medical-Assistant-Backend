namespace SmartHealthcare.API.DTOs.Notifications;

public class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }

    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}