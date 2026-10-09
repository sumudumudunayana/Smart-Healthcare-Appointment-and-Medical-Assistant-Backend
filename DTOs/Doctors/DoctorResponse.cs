namespace SmartHealthcare.API.DTOs.Doctors;

public class DoctorResponse
{
    public Guid DoctorId { get; set; }
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string? Department { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;
    public int Experience { get; set; }

    public string Status { get; set; } = string.Empty;
}