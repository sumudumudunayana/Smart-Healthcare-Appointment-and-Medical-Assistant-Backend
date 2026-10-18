using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Departments;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class DepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET ALL DEPARTMENTS
    // ============================================================

    public async Task<List<DepartmentResponse>> GetAllAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentResponse
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                Location = d.Location
            })
            .ToListAsync();
    }

    // ============================================================
    // CREATE DEPARTMENT
    // ============================================================

    public async Task<DepartmentResponse> CreateAsync(
        CreateDepartmentRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "Department details are required."
            );
        }

        string departmentName =
            request.DepartmentName?.Trim() ?? string.Empty;

        string? description = NormalizeOptionalText(
            request.Description
        );

        string? location = NormalizeOptionalText(
            request.Location
        );

        ValidateDepartmentFields(
            departmentName,
            description,
            location
        );

        bool exists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentName.ToLower() ==
                departmentName.ToLower()
            );

        if (exists)
        {
            throw new InvalidOperationException(
                "A department with this name already exists."
            );
        }

        var department = new Department
        {
            DepartmentId = Guid.NewGuid(),
            DepartmentName = departmentName,
            Description = description,
            Location = location
        };

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        return MapToResponse(department);
    }

    // ============================================================
    // UPDATE DEPARTMENT
    // ============================================================

    public async Task<DepartmentResponse> UpdateAsync(
        Guid departmentId,
        UpdateDepartmentRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "Department details are required."
            );
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.DepartmentId == departmentId
            );

        if (department == null)
        {
            throw new KeyNotFoundException(
                "Department not found."
            );
        }

        string departmentName =
            request.DepartmentName?.Trim() ?? string.Empty;

        string? description = NormalizeOptionalText(
            request.Description
        );

        string? location = NormalizeOptionalText(
            request.Location
        );

        ValidateDepartmentFields(
            departmentName,
            description,
            location
        );

        bool duplicateExists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentId != departmentId &&
                d.DepartmentName.ToLower() ==
                departmentName.ToLower()
            );

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Another department with this name already exists."
            );
        }

        department.DepartmentName = departmentName;
        department.Description = description;
        department.Location = location;

        await _context.SaveChangesAsync();

        return MapToResponse(department);
    }

    // ============================================================
    // DELETE DEPARTMENT
    // ============================================================

    public async Task DeleteAsync(Guid departmentId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.DepartmentId == departmentId
            );

        if (department == null)
        {
            throw new KeyNotFoundException(
                "Department not found."
            );
        }

        // Do not delete a department while doctors are assigned to it.
        bool hasAssignedDoctors = await _context.Doctors
            .AnyAsync(d => d.DepartmentId == departmentId);

        if (hasAssignedDoctors)
        {
            throw new InvalidOperationException(
                "This department cannot be deleted because doctors are assigned to it. Reassign those doctors first."
            );
        }

        _context.Departments.Remove(department);

        await _context.SaveChangesAsync();
    }

    // ============================================================
    // VALIDATION HELPERS
    // ============================================================

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateDepartmentFields(
        string departmentName,
        string? description,
        string? location)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new ArgumentException(
                "Department name is required."
            );
        }

        if (departmentName.Length > 100)
        {
            throw new ArgumentException(
                "Department name cannot exceed 100 characters."
            );
        }

        if (description?.Length > 300)
        {
            throw new ArgumentException(
                "Department description cannot exceed 300 characters."
            );
        }

        if (location?.Length > 150)
        {
            throw new ArgumentException(
                "Department location cannot exceed 150 characters."
            );
        }
    }

    // ============================================================
    // RESPONSE MAPPING
    // ============================================================

    private static DepartmentResponse MapToResponse(
        Department department)
    {
        return new DepartmentResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Description = department.Description,
            Location = department.Location
        };
    }
}