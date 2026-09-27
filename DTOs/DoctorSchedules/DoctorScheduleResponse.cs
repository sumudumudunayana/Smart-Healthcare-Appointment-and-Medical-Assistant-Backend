namespace SmartHealthcare.API.DTOs.DoctorSchedules;

public class DoctorScheduleResponse
{
    public Guid ScheduleId { get; set; }

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string AvailabilityStatus { get; set; } = string.Empty;
}