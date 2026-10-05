namespace SmartHealthcare.API.DTOs.AI;

public class PatientTriageResult
{
    public string UrgencyLevel { get; set; } = string.Empty;

    public string RecommendedSpecialization { get; set; } = string.Empty;

    public bool EmergencyIndicator { get; set; }

    public string Reasoning { get; set; } = string.Empty;
}