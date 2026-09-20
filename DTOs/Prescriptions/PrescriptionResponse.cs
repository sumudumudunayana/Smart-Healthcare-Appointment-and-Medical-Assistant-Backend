namespace SmartHealthcare.API.DTOs.Prescriptions;

public class PrescriptionResponse
{
    public Guid PrescriptionId { get; set; }

    public Guid RecordId { get; set; }

    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public string Medicine { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string? Frequency { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }
}