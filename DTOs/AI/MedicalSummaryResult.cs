namespace SmartHealthcare.API.DTOs.AI;

public class MedicalSummaryResult
{
    public string PatientOverview { get; set; } = string.Empty;

    public List<string> Allergies { get; set; } = new();

    public List<string> ChronicConditions { get; set; } = new();

    public List<string> RecentDiagnoses { get; set; } = new();

    public List<string> RecentTreatments { get; set; } = new();

    public List<string> RecentMedications { get; set; } = new();

    public List<string> RecentLabReports { get; set; } = new();

    public string ClinicalSummary { get; set; } = string.Empty;

    public string Limitations { get; set; } = string.Empty;
}