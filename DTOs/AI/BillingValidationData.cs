namespace SmartHealthcare.API.DTOs.AI;

public class BillingValidationData
{
    public Guid BillId { get; set; }

    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public decimal TotalAmount { get; set; }

    public string BillStatus { get; set; } = string.Empty;

    public DateOnly GeneratedDate { get; set; }

    public string? PatientName { get; set; }

    public DateOnly? AppointmentDate { get; set; }

    public TimeOnly? AppointmentTime { get; set; }

    public string? AppointmentStatus { get; set; }

    public List<BillingPaymentData> Payments { get; set; }
        = new();

    public List<BillingInsuranceClaimData> InsuranceClaims { get; set; }
        = new();

    public List<BillingInsurancePolicyData> InsurancePolicies { get; set; }
        = new();
}

public class BillingPaymentData
{
    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;
}

public class BillingInsuranceClaimData
{
    public Guid ClaimId { get; set; }

    public Guid InsurancePolicyId { get; set; }

    public decimal ClaimAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ClaimDetails { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}

public class BillingInsurancePolicyData
{
    public Guid InsurancePolicyId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal CoverageAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}