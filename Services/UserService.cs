using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Users;

namespace SmartHealthcare.API.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(
        Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        return new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role?.RoleName ?? string.Empty,
            Status = user.Status
        };
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        string fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Full name is required."
            );
        }

        user.FullName = fullName;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : request.Phone.Trim();

        await _context.SaveChangesAsync();

        return new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role?.RoleName ?? string.Empty,
            Status = user.Status
        };
    }



    public async Task<List<UserResponse>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .Select(u => new UserResponse
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role != null
                    ? u.Role.RoleName
                    : string.Empty,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }


    public async Task<UserResponse?> UpdateUserAsync(
    Guid userId,
    AdminUpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        string fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        user.FullName = fullName;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : request.Phone.Trim();

        await _context.SaveChangesAsync();

        return new UserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role?.RoleName ?? string.Empty,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponse?> UpdateUserStatusAsync(
        Guid userId,
        string status)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("User status is required.");
        }

        string normalizedStatus = status.Trim();

        if (!normalizedStatus.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase) &&
            !normalizedStatus.Equals(
                "Inactive",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Status must be either Active or Inactive.");
        }

        user.Status = normalizedStatus.Equals(
            "Active",
            StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : "Inactive";

        await _context.SaveChangesAsync();

        return new UserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role?.RoleName ?? string.Empty,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
}