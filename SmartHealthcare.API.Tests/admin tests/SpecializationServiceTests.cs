using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Specializations;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class SpecializationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SpecializationService _specializationService;

    public SpecializationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _specializationService = new SpecializationService(_context);
    }

    // SPEC-01: Create a specialization with valid details
    [Fact(DisplayName = "SPEC-01 | Create specialization with valid details")]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedSpecialization()
    {
        var request = new CreateSpecializationRequest
        {
            Name = "Cardiology"
        };

        SpecializationResponse response =
            await _specializationService.CreateAsync(request);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.SpecializationId);
        Assert.Equal("Cardiology", response.Name);

        Specialization? savedSpecialization =
            await _context.Specializations
                .FirstOrDefaultAsync(
                    s => s.SpecializationId == response.SpecializationId);

        Assert.NotNull(savedSpecialization);
        Assert.Equal("Cardiology", savedSpecialization.Name);

        Assert.Equal(1, await _context.Specializations.CountAsync());
    }

    // SPEC-02: Retrieve all specializations in alphabetical order
    [Fact(DisplayName = "SPEC-02 | Retrieve all specializations alphabetically")]
    public async Task GetAllAsync_ExistingSpecializations_ReturnsAlphabeticalList()
    {
        _context.Specializations.AddRange(
            new Specialization
            {
                SpecializationId = Guid.NewGuid(),
                Name = "Neurology"
            },
            new Specialization
            {
                SpecializationId = Guid.NewGuid(),
                Name = "Cardiology"
            },
            new Specialization
            {
                SpecializationId = Guid.NewGuid(),
                Name = "Dermatology"
            });

        await _context.SaveChangesAsync();

        List<SpecializationResponse> result =
            await _specializationService.GetAllAsync();

        Assert.Equal(3, result.Count);

        Assert.Collection(
            result,
            item => Assert.Equal("Cardiology", item.Name),
            item => Assert.Equal("Dermatology", item.Name),
            item => Assert.Equal("Neurology", item.Name));
    }

    // SPEC-03: Retrieve specializations when none exist
    [Fact(DisplayName = "SPEC-03 | Retrieve specializations when none exist")]
    public async Task GetAllAsync_NoSpecializations_ReturnsEmptyList()
    {
        List<SpecializationResponse> result =
            await _specializationService.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // SPEC-04: Reject a duplicate specialization name
    [Fact(DisplayName = "SPEC-04 | Reject duplicate specialization name")]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await _specializationService.CreateAsync(
            new CreateSpecializationRequest
            {
                Name = "Cardiology"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _specializationService.CreateAsync(
                new CreateSpecializationRequest
                {
                    Name = "Cardiology"
                }));

        Assert.Equal(
            "A specialization with this name already exists.",
            exception.Message);

        Assert.Equal(1, await _context.Specializations.CountAsync());
    }

    // SPEC-05: Reject a blank specialization name
    [Fact(DisplayName = "SPEC-05 | Reject blank specialization name")]
    public async Task CreateAsync_BlankName_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _specializationService.CreateAsync(
                new CreateSpecializationRequest
                {
                    Name = "   "
                }));

        Assert.Equal(
            "Specialization name is required.",
            exception.Message);

        Assert.Equal(0, await _context.Specializations.CountAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}