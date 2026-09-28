namespace SmartHealthcare.API.DTOs.DoctorSchedules;

public class UpdateDoctorScheduleRequest
{
    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string AvailabilityStatus { get; set; } = "Available";
}