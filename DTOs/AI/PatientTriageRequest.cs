namespace SmartHealthcare.API.DTOs.AI;

public class PatientTriageRequest
{
    public string Symptoms { get; set; } = string.Empty;

    public string? Duration { get; set; }

    public string? AdditionalInformation { get; set; }
}