namespace SmartHealthcare.API.DTOs.Appointments;

public class AppointmentResponse
{
    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public Guid ScheduleId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Symptoms { get; set; }

    public DateTime CreatedAt { get; set; }
}