namespace SmartHealthcare.API.DTOs.AI;

public class AppointmentAvailabilityResult
{
    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public Guid ScheduleId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string Specialization { get; set; } = string.Empty;

    public string? Department { get; set; }
}