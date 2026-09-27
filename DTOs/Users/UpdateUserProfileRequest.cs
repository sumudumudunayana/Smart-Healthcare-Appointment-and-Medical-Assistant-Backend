namespace SmartHealthcare.API.DTOs.Users;

public class UpdateUserProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}