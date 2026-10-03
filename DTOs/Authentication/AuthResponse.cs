namespace SmartHealthcare.API.DTOs.Authentication;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}