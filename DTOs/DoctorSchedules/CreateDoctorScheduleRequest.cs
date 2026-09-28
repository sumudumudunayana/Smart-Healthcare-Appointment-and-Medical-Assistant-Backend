namespace SmartHealthcare.API.DTOs.DoctorSchedules;

public class CreateDoctorScheduleRequest
{
    public Guid DoctorId { get; set; }

    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string AvailabilityStatus { get; set; } = "Available";
}