namespace SmartHealthcare.API.DTOs.Notifications;

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;
}