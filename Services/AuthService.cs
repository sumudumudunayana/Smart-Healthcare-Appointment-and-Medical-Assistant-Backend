using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

        return await GenerateAuthResponseAsync(
            user,
            patientRole.RoleName
        );
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

        return await GenerateAuthResponseAsync(
            user,
            user.Role.RoleName
        );
    }

    // ============================================================
    // REFRESH ACCESS TOKEN
    // ============================================================

    public async Task<AuthResponse> RefreshTokenAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException(
                "Refresh token is required."
            );
        }

        RefreshToken? storedToken = await _context.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (storedToken == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token."
            );
        }

        if (storedToken.RevokedAt.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Refresh token has been revoked."
            );
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(
                "Refresh token has expired."
            );
        }

        if (storedToken.User == null)
        {
            throw new UnauthorizedAccessException(
                "User associated with refresh token was not found."
            );
        }

        User user = storedToken.User;

        if (user.Status != "Active")
        {
            throw new UnauthorizedAccessException(
                "This account is not active."
            );
        }

        if (user.Role == null)
        {
            throw new InvalidOperationException(
                "User role was not found."
            );
        }

        // Revoke the old refresh token.
        storedToken.RevokedAt = DateTime.UtcNow;

        // Generate a completely new authentication response.
        AuthResponse response = await GenerateAuthResponseAsync(
            user,
            user.Role.RoleName
        );

        await _context.SaveChangesAsync();

        return response;
    }

    // ============================================================
    // LOGOUT / REVOKE REFRESH TOKEN
    // ============================================================

    public async Task RevokeRefreshTokenAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        RefreshToken? storedToken =
            await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (storedToken == null)
        {
            return;
        }

        if (!storedToken.RevokedAt.HasValue)
        {
            storedToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    // ============================================================
    // GENERATE AUTH RESPONSE
    // ============================================================

    private async Task<AuthResponse> GenerateAuthResponseAsync(
        User user,
        string role)
    {
        string accessToken = GenerateAccessToken(
            user,
            role
        );

        string refreshTokenValue =
            GenerateRefreshTokenValue();

        int refreshTokenExpiresInDays =
            _configuration.GetValue<int>(
                "Jwt:RefreshTokenExpiresInDays"
            );

        if (refreshTokenExpiresInDays <= 0)
        {
            refreshTokenExpiresInDays = 7;
        }

        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(
                refreshTokenExpiresInDays
            )
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshTokenValue,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role
        };
    }

    // ============================================================
    // GENERATE JWT ACCESS TOKEN
    // ============================================================

    private string GenerateAccessToken(
        User user,
        string role)
    {
        string? jwtKey =
            _configuration["Jwt:Key"];

        string? jwtIssuer =
            _configuration["Jwt:Issuer"];

        string? jwtAudience =
            _configuration["Jwt:Audience"];

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

        if (expiresInMinutes <= 0)
        {
            expiresInMinutes = 60;
        }

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

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // ============================================================
    // GENERATE REFRESH TOKEN
    // ============================================================

    private string GenerateRefreshTokenValue()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
}