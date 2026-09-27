namespace SmartHealthcare.API.DTOs.InsuranceClaims;

public class CreateInsuranceClaimRequest
{
    public Guid InsurancePolicyId { get; set; }
    public Guid BillId { get; set; }
    public decimal ClaimAmount { get; set; }
    public string? ClaimDetails { get; set; }
}