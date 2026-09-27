using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Authentication;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext context,
        PasswordService passwordService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
    }

    // ============================================================
    // REGISTER
    // ============================================================

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists."
            );
        }

        Role? patientRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == "Patient");

        if (patientRole == null)
        {
            throw new InvalidOperationException(
                "Patient role was not found."
            );
        }

        string passwordHash =
            _passwordService.HashPassword(request.Password);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            RoleId = patientRole.RoleId,
            Phone = request.Phone?.Trim(),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            UserId = user.UserId
        };

        _context.Users.Add(user);
        _context.Patients.Add(patient);

        await _context.SaveChangesAsync();

        return GenerateAuthResponse(user, patientRole.RoleName);
    }


    // ============================================================
    // LOGIN
    // ============================================================

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        User? user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password."
            );
        }

        if (user.Status != "Active")
        {
            throw new UnauthorizedAccessException(
                "This account is not active."
            );
        }

        bool passwordValid =
            _passwordService.VerifyPassword(
                request.Password,
                user.PasswordHash
            );

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password."
            );
        }

        if (user.Role == null)
        {
            throw new InvalidOperationException(
                "User role was not found."
            );
        }

        return GenerateAuthResponse(
            user,
            user.Role.RoleName
        );
    }


    // ============================================================
    // JWT GENERATION
    // ============================================================

    private AuthResponse GenerateAuthResponse(
        User user,
        string role)
    {
        string? jwtKey = _configuration["Jwt:Key"];
        string? jwtIssuer = _configuration["Jwt:Issuer"];
        string? jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT key is not configured."
            );
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is not configured."
            );
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT audience is not configured."
            );
        }

        int expiresInMinutes =
            _configuration.GetValue<int>(
                "Jwt:ExpiresInMinutes"
            );

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString()
            ),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email
            ),

            new Claim(
                ClaimTypes.Name,
                user.FullName
            ),

            new Claim(
                ClaimTypes.Role,
                role
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                expiresInMinutes
            ),
            signingCredentials: credentials
        );

        string tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AuthResponse
        {
            Token = tokenString,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role
        };
    }
}