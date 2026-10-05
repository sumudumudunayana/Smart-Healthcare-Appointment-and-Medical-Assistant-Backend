namespace SmartHealthcare.API.DTOs.Bills;

public class CreateBillRequest
{
    public Guid AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }
}