namespace SmartHealthcare.API.Models;

public class Prescription
{
    public Guid PrescriptionId { get; set; }

    public Guid RecordId { get; set; }

    public string Medicine { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string? Frequency { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MedicalRecord? Record { get; set; }
}