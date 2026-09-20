namespace SmartHealthcare.API.DTOs.MedicalRecords;

public class MedicalRecordResponse
{
    public Guid RecordId { get; set; }

    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public Guid AppointmentId { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly AppointmentTime { get; set; }

    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}