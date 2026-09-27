namespace SmartHealthcare.API.DTOs.Bills;

public class BillResponse
{
    public Guid BillId { get; set; }

    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string BillStatus { get; set; } = string.Empty;

    public DateOnly GeneratedDate { get; set; }
}