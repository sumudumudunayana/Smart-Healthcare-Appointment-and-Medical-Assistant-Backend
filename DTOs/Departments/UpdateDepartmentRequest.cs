namespace SmartHealthcare.API.DTOs.Departments;

public class UpdateDepartmentRequest
{
    public string DepartmentName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }
}