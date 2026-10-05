namespace SmartHealthcare.API.Models;

public class Bill
{
    public Guid BillId { get; set; }

    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public decimal TotalAmount { get; set; }

    public string BillStatus { get; set; } = "Pending";

    public DateOnly GeneratedDate { get; set; }

    // Navigation properties

    public Appointment? Appointment { get; set; }

    public Patient? Patient { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<InsuranceClaim> InsuranceClaims { get; set; }
    = new List<InsuranceClaim>();
}