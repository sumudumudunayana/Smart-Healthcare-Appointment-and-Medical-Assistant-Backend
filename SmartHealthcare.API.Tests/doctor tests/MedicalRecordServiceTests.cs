using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.MedicalRecords;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class MedicalRecordServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MedicalRecordService _medicalRecordService;

    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();

    private readonly Guid _appointmentId = Guid.NewGuid();

    private readonly DateOnly _appointmentDate =
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

    private readonly TimeOnly _appointmentTime =
        new(10, 0);

    public MedicalRecordServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _medicalRecordService =
            new MedicalRecordService(_context);

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

        var appointment = new Appointment
        {
            AppointmentId = _appointmentId,
            PatientId = _patientId,
            DoctorId = _doctorId,
            ScheduleId = Guid.NewGuid(),
            AppointmentDate = _appointmentDate,
            AppointmentTime = _appointmentTime,
            Status = "Scheduled",
            Symptoms = "General checkup"
        };

        _context.Users.AddRange(patientUser, doctorUser);
        _context.Patients.Add(patient);
        _context.Doctors.Add(doctor);
        _context.Appointments.Add(appointment);

        _context.SaveChanges();
    }

    private CreateMedicalRecordRequest CreateValidRequest()
    {
        return new CreateMedicalRecordRequest
        {
            AppointmentId = _appointmentId,
            Diagnosis = "Seasonal allergies",
            Treatment = "Antihistamine treatment",
            Notes = "Follow-up if symptoms persist"
        };
    }

    // MED-01: Create a medical record with valid details
    [Fact(DisplayName = "MED-01 | Create medical record with valid details")]
    public async Task CreateAsync_ValidRequest_CreatesMedicalRecord()
    {
        MedicalRecordResponse response =
            await _medicalRecordService.CreateAsync(
                _doctorUserId,
                CreateValidRequest());

        Assert.NotEqual(Guid.Empty, response.RecordId);
        Assert.Equal(_patientId, response.PatientId);
        Assert.Equal("Test Patient", response.PatientName);
        Assert.Equal(_doctorId, response.DoctorId);
        Assert.Equal("Dr. Test Doctor", response.DoctorName);
        Assert.Equal(_appointmentId, response.AppointmentId);
        Assert.Equal("Seasonal allergies", response.Diagnosis);
        Assert.Equal("Antihistamine treatment", response.Treatment);
        Assert.Equal("Follow-up if symptoms persist", response.Notes);

        Assert.Equal(1, await _context.MedicalRecords.CountAsync());
    }

    // MED-02: Reject a nonexistent appointment
    [Fact(DisplayName = "MED-02 | Reject nonexistent appointment")]
    public async Task CreateAsync_InvalidAppointment_ThrowsArgumentException()
    {
        CreateMedicalRecordRequest request = CreateValidRequest();
        request.AppointmentId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _medicalRecordService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal("Appointment was not found.", exception.Message);
        Assert.Empty(await _context.MedicalRecords.ToListAsync());
    }

    // MED-03: Reject another doctor's appointment
    [Fact(DisplayName = "MED-03 | Reject appointment belonging to another doctor")]
    public async Task CreateAsync_OtherDoctorsAppointment_ThrowsInvalidOperationException()
    {
        var otherDoctorUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Dr. Other Doctor",
            Email = "otherdoctor@example.com",
            PasswordHash = "test-hash",
            RoleId = Guid.NewGuid(),
            Status = "Active"
        };

        var otherDoctor = new Doctor
        {
            DoctorId = Guid.NewGuid(),
            UserId = otherDoctorUser.UserId,
            User = otherDoctorUser,
            SpecializationId = Guid.NewGuid(),
            LicenseNumber = "TEST-DOCTOR-002",
            Experience = 8
        };

        var otherAppointment = new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = otherDoctor.DoctorId,
            ScheduleId = Guid.NewGuid(),
            AppointmentDate = _appointmentDate,
            AppointmentTime = new TimeOnly(11, 0),
            Status = "Scheduled"
        };

        _context.Users.Add(otherDoctorUser);
        _context.Doctors.Add(otherDoctor);
        _context.Appointments.Add(otherAppointment);
        await _context.SaveChangesAsync();

        CreateMedicalRecordRequest request = CreateValidRequest();
        request.AppointmentId = otherAppointment.AppointmentId;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _medicalRecordService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal(
            "You can only create a medical record for your own appointments.",
            exception.Message);

        Assert.Empty(await _context.MedicalRecords.ToListAsync());
    }

    // MED-04: Reject a duplicate medical record
    [Fact(DisplayName = "MED-04 | Reject duplicate medical record")]
    public async Task CreateAsync_DuplicateRecord_ThrowsInvalidOperationException()
    {
        await _medicalRecordService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _medicalRecordService.CreateAsync(
                _doctorUserId,
                CreateValidRequest()));

        Assert.Equal(
            "A medical record already exists for this appointment.",
            exception.Message);

        Assert.Equal(1, await _context.MedicalRecords.CountAsync());
    }

    // MED-05: Reject a cancelled appointment
    [Fact(DisplayName = "MED-05 | Reject cancelled appointment")]
    public async Task CreateAsync_CancelledAppointment_ThrowsInvalidOperationException()
    {
        Appointment appointment =
            await _context.Appointments
                .FirstAsync(a => a.AppointmentId == _appointmentId);

        appointment.Status = "Cancelled";
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _medicalRecordService.CreateAsync(
                _doctorUserId,
                CreateValidRequest()));

        Assert.Equal(
            "A medical record cannot be created for a cancelled appointment.",
            exception.Message);

        Assert.Empty(await _context.MedicalRecords.ToListAsync());
    }

    // MED-06: Retrieve a medical record by appointment
    [Fact(DisplayName = "MED-06 | Retrieve medical record by appointment")]
    public async Task GetByAppointmentAsync_ExistingRecord_ReturnsRecord()
    {
        MedicalRecordResponse created =
            await _medicalRecordService.CreateAsync(
                _doctorUserId,
                CreateValidRequest());

        MedicalRecordResponse? result =
            await _medicalRecordService.GetByAppointmentAsync(
                _doctorUserId,
                _appointmentId);

        Assert.NotNull(result);
        Assert.Equal(created.RecordId, result.RecordId);
        Assert.Equal(_appointmentId, result.AppointmentId);
        Assert.Equal("Seasonal allergies", result.Diagnosis);
    }

    // MED-07: Return null when a record does not exist
    [Fact(DisplayName = "MED-07 | Return null for nonexistent record")]
    public async Task GetByAppointmentAsync_NoRecord_ReturnsNull()
    {
        MedicalRecordResponse? result =
            await _medicalRecordService.GetByAppointmentAsync(
                _doctorUserId,
                _appointmentId);

        Assert.Null(result);
    }

    // MED-08: Retrieve records belonging to a doctor
    [Fact(DisplayName = "MED-08 | Retrieve doctor's medical records")]
    public async Task GetMyDoctorRecordsAsync_ReturnsDoctorsRecords()
    {
        await _medicalRecordService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        List<MedicalRecordResponse> result =
            await _medicalRecordService.GetMyDoctorRecordsAsync(
                _doctorUserId);

        Assert.Single(result);
        Assert.Equal(_doctorId, result[0].DoctorId);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Seasonal allergies", result[0].Diagnosis);
    }

    // MED-09: Retrieve records belonging to a patient
    [Fact(DisplayName = "MED-09 | Retrieve patient's medical records")]
    public async Task GetMyRecordsAsync_ReturnsPatientsRecords()
    {
        await _medicalRecordService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        List<MedicalRecordResponse> result =
            await _medicalRecordService.GetMyRecordsAsync(
                _patientUserId);

        Assert.Single(result);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Test Patient", result[0].PatientName);
        Assert.Equal(_doctorId, result[0].DoctorId);
    }

    // MED-10: Normalize empty optional fields
    [Fact(DisplayName = "MED-10 | Store blank optional fields as null")]
    public async Task CreateAsync_BlankOptionalFields_StoresNullValues()
    {
        CreateMedicalRecordRequest request = new()
        {
            AppointmentId = _appointmentId,
            Diagnosis = "   ",
            Treatment = "",
            Notes = null
        };

        MedicalRecordResponse response =
            await _medicalRecordService.CreateAsync(
                _doctorUserId,
                request);

        Assert.Null(response.Diagnosis);
        Assert.Null(response.Treatment);
        Assert.Null(response.Notes);

        MedicalRecord? savedRecord =
            await _context.MedicalRecords
                .FirstOrDefaultAsync(
                    r => r.RecordId == response.RecordId);

        Assert.NotNull(savedRecord);
        Assert.Null(savedRecord.Diagnosis);
        Assert.Null(savedRecord.Treatment);
        Assert.Null(savedRecord.Notes);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}