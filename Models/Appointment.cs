namespace SmartHealthcare.API.Models;

public class Appointment
{
    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid ScheduleId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string Status { get; set; } = "Scheduled";

    public string? Symptoms { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }

    public DoctorSchedule? Schedule { get; set; }
}