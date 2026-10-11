using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.DoctorSchedules;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Tests;

public class DoctorScheduleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DoctorScheduleService _scheduleService;
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _doctorUserId = Guid.NewGuid();

    public DoctorScheduleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _scheduleService = new DoctorScheduleService(_context);

        var doctorUser = new User
        {
            UserId = _doctorUserId,
            FullName = "Dr. Schedule Test",
            Email = "schedule-doctor@example.com",
            PasswordHash = "test-hash",
            RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Status = "Active"
        };

        _context.Users.Add(doctorUser);
        _context.Doctors.Add(new Doctor
        {
            DoctorId = _doctorId,
            UserId = _doctorUserId,
            User = doctorUser,
            SpecializationId = Guid.NewGuid(),
            LicenseNumber = "SCHEDULE-TEST-001",
            Experience = 4
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_NormalizesDayAndAvailability()
    {
        DoctorScheduleResponse response =
            await _scheduleService.CreateAsync(new CreateDoctorScheduleRequest
            {
                DoctorId = _doctorId,
                DayOfWeek = "  monday ",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
                AvailabilityStatus = " unavailable "
            });

        Assert.NotEqual(Guid.Empty, response.ScheduleId);
        Assert.Equal(_doctorId, response.DoctorId);
        Assert.Equal("Monday", response.DayOfWeek);
        Assert.Equal("Unavailable", response.AvailabilityStatus);
        Assert.Equal(1, await _context.DoctorSchedules.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSchedule_ThrowsAndDoesNotAddSchedule()
    {
        var request = new CreateDoctorScheduleRequest
        {
            DoctorId = _doctorId,
            DayOfWeek = "Friday",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            AvailabilityStatus = "Available"
        };

        await _scheduleService.CreateAsync(request);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _scheduleService.CreateAsync(request));

        Assert.Equal(
            "A schedule with the same day and time already exists for this doctor.",
            exception.Message);
        Assert.Equal(1, await _context.DoctorSchedules.CountAsync());
    }

    [Fact]
    public async Task GetDoctorIdFromUserAsync_ExistingDoctor_ReturnsDoctorId()
    {
        Guid? doctorId =
            await _scheduleService.GetDoctorIdFromUserAsync(_doctorUserId);

        Assert.Equal(_doctorId, doctorId);
    }

    [Fact]
    public async Task CreateAsync_InvalidDay_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _scheduleService.CreateAsync(new CreateDoctorScheduleRequest
            {
                DoctorId = _doctorId,
                DayOfWeek = "Funday",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0)
            }));

        Assert.Equal("Invalid day of week.", exception.Message);
        Assert.Empty(await _context.DoctorSchedules.ToListAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
