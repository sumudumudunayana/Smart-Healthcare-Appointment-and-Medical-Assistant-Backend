using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Specializations;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class SpecializationService
{
    private readonly ApplicationDbContext _context;

    public SpecializationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpecializationResponse>> GetAllAsync()
    {
        return await _context.Specializations
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationResponse
            {
                SpecializationId = s.SpecializationId,
                Name = s.Name,
                Description = s.Description
            })
            .ToListAsync();
    }

    public async Task<SpecializationResponse> CreateAsync(
        CreateSpecializationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "Specialization details are required."
            );
        }

        string name = request.Name?.Trim() ?? string.Empty;
        string? description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Specialization name is required."
            );
        }

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Specialization name cannot exceed 100 characters."
            );
        }

        if (description?.Length > 1000)
        {
            throw new ArgumentException(
                "Description cannot exceed 1000 characters."
            );
        }

        bool exists = await _context.Specializations
            .AnyAsync(s => s.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new InvalidOperationException(
                "A specialization with this name already exists."
            );
        }

        var specialization = new Specialization
        {
            SpecializationId = Guid.NewGuid(),
            Name = name,
            Description = description
        };

        _context.Specializations.Add(specialization);

        await _context.SaveChangesAsync();

        return new SpecializationResponse
        {
            SpecializationId = specialization.SpecializationId,
            Name = specialization.Name,
            Description = specialization.Description
        };
    }



    public async Task<SpecializationResponse> UpdateAsync(
    Guid specializationId,
    UpdateSpecializationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "Specialization details are required."
            );
        }

        var specialization = await _context.Specializations
            .FirstOrDefaultAsync(
                s => s.SpecializationId == specializationId
            );

        if (specialization == null)
        {
            throw new KeyNotFoundException(
                "Specialization not found."
            );
        }

        string name = request.Name?.Trim() ?? string.Empty;

        string? description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Specialization name is required."
            );
        }

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Specialization name cannot exceed 100 characters."
            );
        }

        if (description?.Length > 1000)
        {
            throw new ArgumentException(
                "Description cannot exceed 1000 characters."
            );
        }

        bool duplicateExists = await _context.Specializations
            .AnyAsync(s =>
                s.SpecializationId != specializationId &&
                s.Name.ToLower() == name.ToLower()
            );

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Another specialization with this name already exists."
            );
        }

        specialization.Name = name;
        specialization.Description = description;

        await _context.SaveChangesAsync();

        return new SpecializationResponse
        {
            SpecializationId = specialization.SpecializationId,
            Name = specialization.Name,
            Description = specialization.Description
        };
    }

    public async Task DeleteAsync(Guid specializationId)
    {
        var specialization = await _context.Specializations
            .FirstOrDefaultAsync(
                s => s.SpecializationId == specializationId
            );

        if (specialization == null)
        {
            throw new KeyNotFoundException(
                "Specialization not found."
            );
        }

        bool hasDoctors = await _context.Doctors
            .AnyAsync(d => d.SpecializationId == specializationId);

        if (hasDoctors)
        {
            throw new InvalidOperationException(
                "This specialization cannot be deleted because doctors are assigned to it. Reassign those doctors first."
            );
        }

        _context.Specializations.Remove(specialization);

        await _context.SaveChangesAsync();
    }
}