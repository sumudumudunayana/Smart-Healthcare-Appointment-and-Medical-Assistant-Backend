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

    public async Task<DepartmentResponse> CreateAsync(
        CreateDepartmentRequest request)
    {
        string departmentName =
            request.DepartmentName.Trim();

        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new ArgumentException(
                "Department name is required."
            );
        }

        bool exists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentName.ToLower() ==
                departmentName.ToLower());

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
            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
            Location =
                string.IsNullOrWhiteSpace(request.Location)
                    ? null
                    : request.Location.Trim()
        };

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        return new DepartmentResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Description = department.Description,
            Location = department.Location
        };
    }
}