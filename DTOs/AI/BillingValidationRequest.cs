namespace SmartHealthcare.API.DTOs.AI;

public class BillingValidationRequest
{
    public Guid BillId { get; set; }

    public string? AdditionalInstructions { get; set; }
}