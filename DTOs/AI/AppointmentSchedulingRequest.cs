namespace SmartHealthcare.API.DTOs.AI;

public class AppointmentSchedulingRequest
{
    public string? Specialization { get; set; }

    public string? PreferredDate { get; set; }

    public string? PreferredTimePeriod { get; set; }

    public string? AdditionalPreferences { get; set; }
}