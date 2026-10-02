namespace SmartHealthcare.API.DTOs.AI;

public class AppointmentSchedulingResult
{
    public string? Specialization { get; set; }

    public DateOnly? PreferredDate { get; set; }

    public string? PreferredTimePeriod { get; set; }

    public string? AdditionalPreferences { get; set; }

    public string? Reasoning { get; set; }
}