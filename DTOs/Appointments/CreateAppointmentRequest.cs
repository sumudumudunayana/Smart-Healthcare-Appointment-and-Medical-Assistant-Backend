namespace SmartHealthcare.API.DTOs.Appointments;

public class CreateAppointmentRequest
{
    public Guid DoctorId { get; set; }

    public Guid ScheduleId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string? Symptoms { get; set; }
}