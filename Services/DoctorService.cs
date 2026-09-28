using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Doctors;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class DoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public DoctorService(
        ApplicationDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<DoctorResponse> CreateAsync(
        CreateDoctorRequest request)
    {
        string fullName = request.FullName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();
        string licenseNumber = request.LicenseNumber.Trim();

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

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new ArgumentException(
                "License number is required."
            );
        }

        if (request.Experience < 0)
        {
            throw new ArgumentException(
                "Experience cannot be negative."
            );
        }

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists."
            );
        }

        bool licenseExists = await _context.Doctors
            .AnyAsync(d =>
                d.LicenseNumber.ToLower() ==
                licenseNumber.ToLower());

        if (licenseExists)
        {
            throw new InvalidOperationException(
                "A doctor with this license number already exists."
            );
        }

        Role? doctorRole = await _context.Roles
            .FirstOrDefaultAsync(r =>
                r.RoleName == "Doctor");

        if (doctorRole == null)
        {
            throw new InvalidOperationException(
                "Doctor role was not found."
            );
        }

        Specialization? specialization =
            await _context.Specializations
                .FirstOrDefaultAsync(s =>
                    s.SpecializationId ==
                    request.SpecializationId);

        if (specialization == null)
        {
            throw new ArgumentException(
                "The selected specialization was not found."
            );
        }

        if (request.DepartmentId.HasValue)
        {
            bool departmentExists =
                await _context.Departments.AnyAsync(d =>
                    d.DepartmentId ==
                    request.DepartmentId.Value);

            if (!departmentExists)
            {
                throw new ArgumentException(
                    "The selected department was not found."
                );
            }
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
            RoleId = doctorRole.RoleId,
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            DoctorId = Guid.NewGuid(),
            UserId = user.UserId,
            SpecializationId =
                request.SpecializationId,
            DepartmentId =
                request.DepartmentId,
            LicenseNumber = licenseNumber,
            Experience = request.Experience
        };

        _context.Users.Add(user);
        _context.Doctors.Add(doctor);

        await _context.SaveChangesAsync();

        return await BuildDoctorResponseAsync(
            doctor.DoctorId);
    }

    public async Task<List<DoctorResponse>> GetAllAsync(
    string? search,
    Guid? specializationId,
    Guid? departmentId)
    {
        IQueryable<Doctor> query =
            _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Department);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchTerm =
                search.Trim().ToLower();

            query = query.Where(d =>
                d.User!.FullName.ToLower()
                    .Contains(searchTerm)
                ||
                d.Specialization!.Name.ToLower()
                    .Contains(searchTerm)
                ||
                (d.Department != null &&
                 d.Department.DepartmentName
                    .ToLower()
                    .Contains(searchTerm))
            );
        }

        if (specializationId.HasValue)
        {
            query = query.Where(d =>
                d.SpecializationId ==
                specializationId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(d =>
                d.DepartmentId ==
                departmentId.Value);
        }

        return await query
            .OrderBy(d => d.User!.FullName)
            .Select(d => new DoctorResponse
            {
                DoctorId = d.DoctorId,
                UserId = d.UserId,
                FullName = d.User!.FullName,
                Email = d.User.Email,
                Phone = d.User.Phone,
                Specialization =
                    d.Specialization!.Name,
                Department =
                    d.Department != null
                        ? d.Department.DepartmentName
                        : null,
                LicenseNumber = d.LicenseNumber,
                Experience = d.Experience,
                Status = d.User.Status
            })
            .ToListAsync();
    }

    public async Task<DoctorResponse?> GetByIdAsync(
        Guid doctorId)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Specialization)
            .Include(d => d.Department)
            .Where(d => d.DoctorId == doctorId)
            .Select(d => new DoctorResponse
            {
                DoctorId = d.DoctorId,
                UserId = d.UserId,
                FullName = d.User!.FullName,
                Email = d.User.Email,
                Phone = d.User.Phone,
                Specialization =
                    d.Specialization!.Name,
                Department =
                    d.Department != null
                        ? d.Department.DepartmentName
                        : null,
                LicenseNumber = d.LicenseNumber,
                Experience = d.Experience,
                Status = d.User.Status
            })
            .FirstOrDefaultAsync();
    }

    private async Task<DoctorResponse>
        BuildDoctorResponseAsync(Guid doctorId)
    {
        DoctorResponse? doctor =
            await GetByIdAsync(doctorId);

        if (doctor == null)
        {
            throw new InvalidOperationException(
                "The doctor could not be retrieved after creation."
            );
        }

        return doctor;
    }
}