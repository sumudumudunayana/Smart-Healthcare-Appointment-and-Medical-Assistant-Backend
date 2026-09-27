namespace SmartHealthcare.API.DTOs.InsurancePolicies;

public class InsurancePolicyResponse
{
    public Guid InsurancePolicyId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal CoverageAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}