using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Appointments;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AppointmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AppointmentService _appointmentService;

    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();

    private readonly Guid _scheduleId = Guid.NewGuid();

    private readonly DateOnly _appointmentDate;
    private readonly TimeOnly _appointmentTime = new(10, 0);

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _appointmentService = new AppointmentService(_context);

        // Find a future date that matches the schedule day.
        DateOnly date = DateOnly.FromDateTime(DateTime.UtcNow)
            .AddDays(1);

        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        _appointmentDate = date;

        SeedData();
    }

    private void SeedData()
    {
        var patientUser = new User
        {
            UserId = _patientUserId,
            FullName = "Test Patient",
            Email = "patient@example.com",
            PasswordHash = "test-hash",
            RoleId = Guid.NewGuid(),
            Status = "Active"
        };

        var doctorUser = new User
        {
            UserId = _doctorUserId,
            FullName = "Dr. Test Doctor",
            Email = "doctor@example.com",
            PasswordHash = "test-hash",
            RoleId = Guid.NewGuid(),
            Status = "Active"
        };

        var patient = new Patient
        {
            PatientId = _patientId,
            UserId = _patientUserId,
            User = patientUser
        };

        var doctor = new Doctor
        {
            DoctorId = _doctorId,
            UserId = _doctorUserId,
            User = doctorUser,
            SpecializationId = Guid.NewGuid(),
            LicenseNumber = "TEST-DOCTOR-001",
            Experience = 5
        };

        var schedule = new DoctorSchedule
        {
            ScheduleId = _scheduleId,
            DoctorId = _doctorId,
            Doctor = doctor,
            DayOfWeek = "Monday",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            AvailabilityStatus = "Available"
        };

        _context.Users.AddRange(patientUser, doctorUser);
        _context.Patients.Add(patient);
        _context.Doctors.Add(doctor);
        _context.DoctorSchedules.Add(schedule);

        _context.SaveChanges();
    }

    private CreateAppointmentRequest CreateValidRequest()
    {
        return new CreateAppointmentRequest
        {
            DoctorId = _doctorId,
            ScheduleId = _scheduleId,
            AppointmentDate = _appointmentDate,
            AppointmentTime = _appointmentTime,
            Symptoms = "Regular medical consultation"
        };
    }

    // APT-01: Create an appointment with valid details
    [Fact(DisplayName = "APT-01 | Create appointment with valid details")]
    public async Task CreateAsync_ValidRequest_CreatesAppointment()
    {
        AppointmentResponse response =
            await _appointmentService.CreateAsync(
                _patientUserId,
                CreateValidRequest());

        Assert.NotEqual(Guid.Empty, response.AppointmentId);
        Assert.Equal(_patientId, response.PatientId);
        Assert.Equal(_doctorId, response.DoctorId);
        Assert.Equal(_scheduleId, response.ScheduleId);
        Assert.Equal(_appointmentDate, response.AppointmentDate);
        Assert.Equal(_appointmentTime, response.AppointmentTime);
        Assert.Equal("Scheduled", response.Status);
        Assert.Equal("Test Patient", response.PatientName);
        Assert.Equal("Dr. Test Doctor", response.DoctorName);
        Assert.Equal(
            "Regular medical consultation",
            response.Symptoms);

        Assert.Equal(1, await _context.Appointments.CountAsync());
    }

    // APT-02: Reject a nonexistent doctor
    [Fact(DisplayName = "APT-02 | Reject nonexistent doctor")]
    public async Task CreateAsync_InvalidDoctor_ThrowsArgumentException()
    {
        CreateAppointmentRequest request = CreateValidRequest();
        request.DoctorId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CreateAsync(
                _patientUserId,
                request));

        Assert.Equal(
            "The selected doctor was not found.",
            exception.Message);

        Assert.Empty(await _context.Appointments.ToListAsync());
    }

    // APT-03: Reject a nonexistent schedule
    [Fact(DisplayName = "APT-03 | Reject nonexistent schedule")]
    public async Task CreateAsync_InvalidSchedule_ThrowsArgumentException()
    {
        CreateAppointmentRequest request = CreateValidRequest();
        request.ScheduleId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CreateAsync(
                _patientUserId,
                request));

        Assert.Equal(
            "The selected schedule was not found.",
            exception.Message);

        Assert.Empty(await _context.Appointments.ToListAsync());
    }

    // APT-04: Reject a schedule belonging to another doctor
    [Fact(DisplayName = "APT-04 | Reject schedule belonging to another doctor")]
    public async Task CreateAsync_ScheduleBelongsToAnotherDoctor_ThrowsArgumentException()
    {
        CreateAppointmentRequest request = CreateValidRequest();

        request.DoctorId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CreateAsync(
                _patientUserId,
                request));

        Assert.Equal(
            "The selected doctor was not found.",
            exception.Message);

        Assert.Empty(await _context.Appointments.ToListAsync());
    }

    // APT-05: Reject an appointment in the past
    [Fact(DisplayName = "APT-05 | Reject appointment in the past")]
    public async Task CreateAsync_PastDate_ThrowsArgumentException()
    {
        CreateAppointmentRequest request = CreateValidRequest();

        request.AppointmentDate =
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CreateAsync(
                _patientUserId,
                request));

        Assert.Equal(
            "Appointment date cannot be in the past.",
            exception.Message);

        Assert.Empty(await _context.Appointments.ToListAsync());
    }

    // APT-06: Reject a time outside the schedule
    [Fact(DisplayName = "APT-06 | Reject appointment outside schedule")]
    public async Task CreateAsync_TimeOutsideSchedule_ThrowsArgumentException()
    {
        CreateAppointmentRequest request = CreateValidRequest();

        request.AppointmentTime = new TimeOnly(18, 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CreateAsync(
                _patientUserId,
                request));

        Assert.Equal(
            "The selected appointment time is outside the doctor's schedule.",
            exception.Message);

        Assert.Empty(await _context.Appointments.ToListAsync());
    }

    // APT-07: Reject a double booking
    [Fact(DisplayName = "APT-07 | Reject double booking")]
    public async Task CreateAsync_AlreadyBookedTime_ThrowsInvalidOperationException()
    {
        await _appointmentService.CreateAsync(
            _patientUserId,
            CreateValidRequest());

        var anotherPatientUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Another Patient",
            Email = "anotherpatient@example.com",
            PasswordHash = "test-hash",
            RoleId = Guid.NewGuid(),
            Status = "Active"
        };

        var anotherPatient = new Patient
        {
            PatientId = Guid.NewGuid(),
            UserId = anotherPatientUser.UserId,
            User = anotherPatientUser
        };

        _context.Users.Add(anotherPatientUser);
        _context.Patients.Add(anotherPatient);
        await _context.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _appointmentService.CreateAsync(
                    anotherPatientUser.UserId,
                    CreateValidRequest()));

        Assert.Equal(
            "The selected appointment time is already booked.",
            exception.Message);

        Assert.Equal(1, await _context.Appointments.CountAsync());
    }

    // APT-08: Retrieve the patient's appointments
    [Fact(DisplayName = "APT-08 | Retrieve patient appointments")]
    public async Task GetMyAppointmentsAsync_ExistingAppointments_ReturnsAppointments()
    {
        await _appointmentService.CreateAsync(
            _patientUserId,
            CreateValidRequest());

        List<AppointmentResponse> result =
            await _appointmentService.GetMyAppointmentsAsync(
                _patientUserId);

        Assert.Single(result);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Test Patient", result[0].PatientName);
        Assert.Equal("Dr. Test Doctor", result[0].DoctorName);
    }

    // APT-09: Retrieve an appointment by ID
    [Fact(DisplayName = "APT-09 | Retrieve appointment by ID")]
    public async Task GetByIdAsync_ExistingAppointment_ReturnsAppointment()
    {
        AppointmentResponse created =
            await _appointmentService.CreateAsync(
                _patientUserId,
                CreateValidRequest());

        AppointmentResponse? result =
            await _appointmentService.GetByIdAsync(
                _patientUserId,
                created.AppointmentId);

        Assert.NotNull(result);
        Assert.Equal(created.AppointmentId, result.AppointmentId);
        Assert.Equal(_patientId, result.PatientId);
    }

    // APT-10: Cancel a scheduled appointment
    [Fact(DisplayName = "APT-10 | Cancel scheduled appointment")]
    public async Task CancelAsync_ScheduledAppointment_ChangesStatusToCancelled()
    {
        AppointmentResponse created =
            await _appointmentService.CreateAsync(
                _patientUserId,
                CreateValidRequest());

        AppointmentResponse? result =
            await _appointmentService.CancelAsync(
                _patientUserId,
                created.AppointmentId);

        Assert.NotNull(result);
        Assert.Equal("Cancelled", result.Status);

        Appointment? saved =
            await _context.Appointments
                .FirstOrDefaultAsync(
                    a => a.AppointmentId == created.AppointmentId);

        Assert.NotNull(saved);
        Assert.Equal("Cancelled", saved.Status);
    }

    // APT-11: Reject cancellation of an already cancelled appointment
    [Fact(DisplayName = "APT-11 | Reject cancellation of an already cancelled appointment")]
    public async Task CancelAsync_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        AppointmentResponse created =
            await _appointmentService.CreateAsync(
                _patientUserId,
                CreateValidRequest());

        await _appointmentService.CancelAsync(
            _patientUserId,
            created.AppointmentId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _appointmentService.CancelAsync(
                    _patientUserId,
                    created.AppointmentId));

        Assert.Equal(
            "This appointment has already been cancelled.",
            exception.Message);
    }

    // APT-12: Return null when an appointment does not exist
    [Fact(DisplayName = "APT-12 | Return null for nonexistent appointment")]
    public async Task GetByIdAsync_NonexistentAppointment_ReturnsNull()
    {
        AppointmentResponse? result =
            await _appointmentService.GetByIdAsync(
                _patientUserId,
                Guid.NewGuid());

        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}