namespace SmartHealthcare.API.Models;

public class User
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public string? Phone { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Role? Role { get; set; }

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }

    public ICollection<Notification> Notifications { get; set; }
    = new List<Notification>();

    public ICollection<AIApproval> AIApprovals { get; set; }
        = new List<AIApproval>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
    = new List<RefreshToken>();
}