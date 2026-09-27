namespace SmartHealthcare.API.Models;

public class InsuranceClaim
{
    public Guid ClaimId { get; set; }

    public Guid InsurancePolicyId { get; set; }

    public Guid BillId { get; set; }

    public decimal ClaimAmount { get; set; }

    public string Status { get; set; } = "Pending";

    public string? ClaimDetails { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public InsurancePolicy? InsurancePolicy { get; set; }

    public Bill? Bill { get; set; }
}