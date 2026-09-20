namespace SmartHealthcare.API.Models;

public class ChronicCondition
{
    public Guid ConditionId { get; set; }

    public Guid PatientId { get; set; }

    public string ConditionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DiagnosedDate { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
}