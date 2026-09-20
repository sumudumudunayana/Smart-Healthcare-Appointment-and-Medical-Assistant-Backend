namespace SmartHealthcare.API.DTOs.Prescriptions;

public class CreatePrescriptionRequest
{
    public Guid RecordId { get; set; }

    public string Medicine { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string? Frequency { get; set; }

    public string? Instructions { get; set; }
}