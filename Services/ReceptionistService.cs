using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Receptionists;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class ReceptionistService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public ReceptionistService(
        ApplicationDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    // CREATE RECEPTIONIST

    public async Task<ReceptionistResponse> CreateAsync(
        CreateReceptionistRequest request)
    {
        string fullName = request.FullName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Full name is required."
            );
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Password is required."
            );
        }

        bool emailExists = await _context.Users
            .AnyAsync(u =>
                u.Email.ToLower() == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists."
            );
        }

        Role? receptionistRole = await _context.Roles
            .FirstOrDefaultAsync(r =>
                r.RoleName == "Receptionist");

        if (receptionistRole == null)
        {
            throw new InvalidOperationException(
                "Receptionist role was not found."
            );
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),

            FullName = fullName,

            Email = email,

            PasswordHash =
                _passwordService.HashPassword(
                    request.Password
                ),

            RoleId = receptionistRole.RoleId,

            Phone = string.IsNullOrWhiteSpace(
                request.Phone)
                ? null
                : request.Phone.Trim(),

            Status = "Active",

            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return new ReceptionistResponse
        {
            UserId = user.UserId,

            FullName = user.FullName,

            Email = user.Email,

            Phone = user.Phone,

            Status = user.Status,

            CreatedAt = user.CreatedAt
        };
    }

    // GET ALL RECEPTIONISTS

    public async Task<List<ReceptionistResponse>>
        GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == "Receptionist")
            .OrderBy(u => u.FullName)
            .Select(u => new ReceptionistResponse
            {
                UserId = u.UserId,

                FullName = u.FullName,

                Email = u.Email,

                Phone = u.Phone,

                Status = u.Status,

                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }
}