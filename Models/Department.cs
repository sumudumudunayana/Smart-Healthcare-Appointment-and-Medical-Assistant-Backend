namespace SmartHealthcare.API.Models;

public class Department
{
    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public ICollection<Doctor> Doctors { get; set; }
        = new List<Doctor>();
}