namespace SmartHealthcare.API.Models;

public class MedicalRecord
{
    public Guid RecordId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid AppointmentId { get; set; }

    public string? Diagnosis { get; set; }

    public string? Treatment { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }

    public Appointment? Appointment { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public ICollection<LabReport> LabReports { get; set; } = new List<LabReport>();
}