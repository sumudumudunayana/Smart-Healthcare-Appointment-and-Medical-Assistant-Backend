namespace SmartHealthcare.API.DTOs.MedicalRecords;

public class CreateMedicalRecordRequest
{
    public Guid AppointmentId { get; set; }

    public string? Diagnosis { get; set; }

    public string? Treatment { get; set; }

    public string? Notes { get; set; }
}