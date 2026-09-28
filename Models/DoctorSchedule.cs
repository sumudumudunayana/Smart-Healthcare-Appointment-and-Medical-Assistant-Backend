namespace SmartHealthcare.API.Models;

public class DoctorSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid DoctorId { get; set; }

    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string AvailabilityStatus { get; set; } = "Available";

    // Navigation property

    public Doctor? Doctor { get; set; }
}