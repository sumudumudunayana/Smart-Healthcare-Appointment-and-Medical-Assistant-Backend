namespace SmartHealthcare.API.DTOs.Payments;

public class CreatePaymentRequest
{
    public Guid BillId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}