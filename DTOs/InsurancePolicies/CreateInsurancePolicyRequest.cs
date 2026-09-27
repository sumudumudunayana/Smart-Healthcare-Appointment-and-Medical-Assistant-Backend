namespace SmartHealthcare.API.DTOs.InsurancePolicies;

public class CreateInsurancePolicyRequest
{
    public Guid PatientId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal CoverageAmount { get; set; }
}