namespace SmartHealthcare.API.DTOs.Payments;

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public Guid BillId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal BillAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string BillStatus { get; set; } = string.Empty;
}