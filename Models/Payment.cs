namespace SmartHealthcare.API.Models;

public class Payment
{
    public Guid PaymentId { get; set; }

    public Guid BillId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; }

    public string PaymentStatus { get; set; } = "Pending";

    public Bill? Bill { get; set; }
}