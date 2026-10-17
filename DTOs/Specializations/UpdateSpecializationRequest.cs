namespace SmartHealthcare.API.DTOs.Specializations;

public class UpdateSpecializationRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}