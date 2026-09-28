namespace SmartHealthcare.API.DTOs.Doctors;

public class CreateDoctorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;

    public Guid SpecializationId { get; set; }
    public Guid? DepartmentId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;
    public int Experience { get; set; }
}