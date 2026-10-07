namespace SmartHealthcare.API.DTOs.AI;

public class MedicalSummaryRequest
{
    public Guid PatientId { get; set; }

    public string? AdditionalInstructions { get; set; }
}