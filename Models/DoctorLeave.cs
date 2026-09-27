namespace SmartHealthcare.API.Models;

public class DoctorLeave
{
    public Guid LeaveId { get; set; }

    public Guid DoctorId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = "Pending";

    // Navigation property

    public Doctor? Doctor { get; set; }
}