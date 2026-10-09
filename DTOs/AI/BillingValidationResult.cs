namespace SmartHealthcare.API.DTOs.AI;

public class BillingValidationResult
{
    public bool IsValid { get; set; }

    public string OverallAssessment { get; set; } = string.Empty;

    public List<string> Issues { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<string> ValidationChecks { get; set; } = new();

    public bool RequiresHumanReview { get; set; }

    public string Reasoning { get; set; } = string.Empty;
}