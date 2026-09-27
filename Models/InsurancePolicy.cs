namespace SmartHealthcare.API.Models;

public class InsurancePolicy
{
    public Guid InsurancePolicyId { get; set; }

    public Guid PatientId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal CoverageAmount { get; set; }

    public string Status { get; set; } = "Active";

    public Patient? Patient { get; set; }

    public ICollection<InsuranceClaim> InsuranceClaims { get; set; }
        = new List<InsuranceClaim>();
}