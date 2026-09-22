namespace SmartHealthcare.API.Models;

public class PatientAllergy
{
    public Guid AllergyId { get; set; }

    public Guid PatientId { get; set; }

    public string AllergyName { get; set; } = string.Empty;

    public string? Reaction { get; set; }

    public string? Severity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
}