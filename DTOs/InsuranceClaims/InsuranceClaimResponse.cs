namespace SmartHealthcare.API.DTOs.InsuranceClaims;

public class InsuranceClaimResponse
{
    public Guid ClaimId { get; set; }

    public Guid InsurancePolicyId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;

    public Guid BillId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public decimal BillAmount { get; set; }
    public decimal ClaimAmount { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? ClaimDetails { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}