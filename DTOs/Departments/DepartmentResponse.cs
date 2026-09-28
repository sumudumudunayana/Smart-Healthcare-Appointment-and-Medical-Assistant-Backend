namespace SmartHealthcare.API.DTOs.Departments;

public class DepartmentResponse
{
    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; }
        = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }
}