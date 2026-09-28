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
                Name = s.Name
            })
            .ToListAsync();
    }

    public async Task<SpecializationResponse> CreateAsync(
        CreateSpecializationRequest request)
    {
        string name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Specialization name is required."
            );
        }

        bool exists = await _context.Specializations
            .AnyAsync(s =>
                s.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new InvalidOperationException(
                "A specialization with this name already exists."
            );
        }

        var specialization = new Specialization
        {
            SpecializationId = Guid.NewGuid(),
            Name = name
        };

        _context.Specializations.Add(specialization);

        await _context.SaveChangesAsync();

        return new SpecializationResponse
        {
            SpecializationId = specialization.SpecializationId,
            Name = specialization.Name
        };
    }
}