using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Departments;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class DepartmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DepartmentService _departmentService;

    public DepartmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _departmentService = new DepartmentService(_context);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_TrimsAndPersistsDepartment()
    {
        DepartmentResponse response =
            await _departmentService.CreateAsync(new CreateDepartmentRequest
            {
                DepartmentName = "  Cardiology  ",
                Description = "  Heart care  ",
                Location = "  Building A  "
            });

        Assert.NotEqual(Guid.Empty, response.DepartmentId);
        Assert.Equal("Cardiology", response.DepartmentName);
        Assert.Equal("Heart care", response.Description);
        Assert.Equal("Building A", response.Location);
        Assert.Equal(1, await _context.Departments.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameIgnoringCase_ThrowsAndDoesNotAddDepartment()
    {
        await _departmentService.CreateAsync(new CreateDepartmentRequest
        {
            DepartmentName = "Cardiology"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _departmentService.CreateAsync(new CreateDepartmentRequest
            {
                DepartmentName = "  CARDIOLOGY "
            }));

        Assert.Equal(
            "A department with this name already exists.",
            exception.Message);
        Assert.Equal(1, await _context.Departments.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentsAlphabetically()
    {
        await _departmentService.CreateAsync(new CreateDepartmentRequest
        {
            DepartmentName = "Neurology"
        });
        await _departmentService.CreateAsync(new CreateDepartmentRequest
        {
            DepartmentName = "Cardiology"
        });

        List<DepartmentResponse> departments =
            await _departmentService.GetAllAsync();

        Assert.Collection(
            departments,
            department => Assert.Equal("Cardiology", department.DepartmentName),
            department => Assert.Equal("Neurology", department.DepartmentName));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
