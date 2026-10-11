using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Prescriptions;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class PrescriptionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PrescriptionService _prescriptionService;

    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();

    private readonly Guid _recordId = Guid.NewGuid();
    private readonly Guid _appointmentId = Guid.NewGuid();

    public PrescriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _prescriptionService = new PrescriptionService(_context);

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
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                .AddDays(7),
            AppointmentTime = new TimeOnly(10, 0),
            Status = "Completed",
            Symptoms = "General checkup",
            Patient = patient,
            Doctor = doctor
        };

        var medicalRecord = new MedicalRecord
        {
            RecordId = _recordId,
            PatientId = _patientId,
            DoctorId = _doctorId,
            AppointmentId = _appointmentId,
            Diagnosis = "Seasonal allergies",
            Treatment = "General treatment",
            Notes = "Routine medical record",
            Patient = patient,
            Doctor = doctor,
            Appointment = appointment
        };

        _context.Users.AddRange(patientUser, doctorUser);
        _context.Patients.Add(patient);
        _context.Doctors.Add(doctor);
        _context.Appointments.Add(appointment);
        _context.MedicalRecords.Add(medicalRecord);

        _context.SaveChanges();
    }

    private CreatePrescriptionRequest CreateValidRequest()
    {
        return new CreatePrescriptionRequest
        {
            RecordId = _recordId,
            Medicine = "Cetirizine",
            Dosage = "10 mg",
            Duration = "5 days",
            Frequency = "Once daily",
            Instructions = "Take after dinner"
        };
    }

    // PRES-01
    [Fact(DisplayName = "PRES-01 | Create prescription with valid details")]
    public async Task CreateAsync_ValidRequest_CreatesPrescription()
    {
        CreatePrescriptionRequest request = CreateValidRequest();

        PrescriptionResponse response =
            await _prescriptionService.CreateAsync(
                _doctorUserId,
                request);

        Assert.NotEqual(Guid.Empty, response.PrescriptionId);

        Assert.Equal(_recordId, response.RecordId);

        Assert.Equal(_patientId, response.PatientId);
        Assert.Equal("Test Patient", response.PatientName);

        Assert.Equal(_doctorId, response.DoctorId);
        Assert.Equal("Dr. Test Doctor", response.DoctorName);

        Assert.Equal("Cetirizine", response.Medicine);
        Assert.Equal("10 mg", response.Dosage);
        Assert.Equal("5 days", response.Duration);
        Assert.Equal("Once daily", response.Frequency);
        Assert.Equal("Take after dinner", response.Instructions);

        Assert.Equal(
            1,
            await _context.Prescriptions.CountAsync());
    }

    // PRES-02
    [Fact(DisplayName = "PRES-02 | Reject creation without doctor profile")]
    public async Task CreateAsync_MissingDoctor_ThrowsInvalidOperationException()
    {
        CreatePrescriptionRequest request = CreateValidRequest();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _prescriptionService.CreateAsync(
                    Guid.NewGuid(),
                    request));

        Assert.Equal(
            "Doctor profile was not found.",
            exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-03
    [Fact(DisplayName = "PRES-03 | Reject empty medicine")]
    public async Task CreateAsync_EmptyMedicine_ThrowsArgumentException()
    {
        CreatePrescriptionRequest request = CreateValidRequest();
        request.Medicine = " ";

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _prescriptionService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal("Medicine is required.", exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-04
    [Fact(DisplayName = "PRES-04 | Reject empty dosage")]
    public async Task CreateAsync_EmptyDosage_ThrowsArgumentException()
    {
        CreatePrescriptionRequest request = CreateValidRequest();
        request.Dosage = "";

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _prescriptionService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal("Dosage is required.", exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-05
    [Fact(DisplayName = "PRES-05 | Reject empty duration")]
    public async Task CreateAsync_EmptyDuration_ThrowsArgumentException()
    {
        CreatePrescriptionRequest request = CreateValidRequest();
        request.Duration = "   ";

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _prescriptionService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal("Duration is required.", exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-06
    [Fact(DisplayName = "PRES-06 | Reject nonexistent medical record")]
    public async Task CreateAsync_MissingRecord_ThrowsArgumentException()
    {
        CreatePrescriptionRequest request = CreateValidRequest();
        request.RecordId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _prescriptionService.CreateAsync(
                _doctorUserId,
                request));

        Assert.Equal(
            "Medical record was not found.",
            exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-07
    [Fact(DisplayName = "PRES-07 | Reject another doctor's medical record")]
    public async Task CreateAsync_OtherDoctorsRecord_ThrowsInvalidOperationException()
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

        // Create a separate appointment for the other doctor.
        var otherAppointment = new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = otherDoctor.DoctorId,
            ScheduleId = Guid.NewGuid(),
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                .AddDays(10),
            AppointmentTime = new TimeOnly(11, 0),
            Status = "Completed",
            Symptoms = "Follow-up",
            Patient = await _context.Patients
                .FirstAsync(p => p.PatientId == _patientId),
            Doctor = otherDoctor
        };

        var otherRecord = new MedicalRecord
        {
            RecordId = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = otherDoctor.DoctorId,
            AppointmentId = otherAppointment.AppointmentId,
            Diagnosis = "Another doctor's diagnosis",
            Patient = otherAppointment.Patient,
            Doctor = otherDoctor,
            Appointment = otherAppointment
        };

        _context.Users.Add(otherDoctorUser);
        _context.Doctors.Add(otherDoctor);
        _context.Appointments.Add(otherAppointment);
        _context.MedicalRecords.Add(otherRecord);

        await _context.SaveChangesAsync();

        // Attempt to prescribe for the other doctor's medical record.
        CreatePrescriptionRequest request = CreateValidRequest();
        request.RecordId = otherRecord.RecordId;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _prescriptionService.CreateAsync(
                    _doctorUserId,
                    request));

        Assert.Equal(
            "You can only create prescriptions for your own medical records.",
            exception.Message);

        Assert.Empty(await _context.Prescriptions.ToListAsync());
    }

    // PRES-08
    [Fact(DisplayName = "PRES-08 | Trim input and normalize optional fields")]
    public async Task CreateAsync_WhitespaceFields_TrimsAndStoresNull()
    {
        CreatePrescriptionRequest request = new()
        {
            RecordId = _recordId,
            Medicine = "  Cetirizine  ",
            Dosage = "  10 mg  ",
            Duration = "  5 days  ",
            Frequency = "   ",
            Instructions = ""
        };

        PrescriptionResponse response =
            await _prescriptionService.CreateAsync(
                _doctorUserId,
                request);

        Assert.Equal("Cetirizine", response.Medicine);
        Assert.Equal("10 mg", response.Dosage);
        Assert.Equal("5 days", response.Duration);

        Assert.Null(response.Frequency);
        Assert.Null(response.Instructions);

        Prescription savedPrescription =
            await _context.Prescriptions.FirstAsync();

        Assert.Equal("Cetirizine", savedPrescription.Medicine);
        Assert.Equal("10 mg", savedPrescription.Dosage);
        Assert.Equal("5 days", savedPrescription.Duration);

        Assert.Null(savedPrescription.Frequency);
        Assert.Null(savedPrescription.Instructions);
    }

    // PRES-09
    [Fact(DisplayName = "PRES-09 | Retrieve prescriptions by medical record")]
    public async Task GetByRecordAsync_ExistingPrescriptions_ReturnsPrescriptions()
    {
        await _prescriptionService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        List<PrescriptionResponse> result =
            await _prescriptionService.GetByRecordAsync(
                _doctorUserId,
                _recordId);

        Assert.Single(result);

        Assert.Equal(_recordId, result[0].RecordId);
        Assert.Equal("Cetirizine", result[0].Medicine);
        Assert.Equal("Dr. Test Doctor", result[0].DoctorName);
        Assert.Equal("Test Patient", result[0].PatientName);
    }

    // PRES-10
    [Fact(DisplayName = "PRES-10 | Return empty list when record has no prescriptions")]
    public async Task GetByRecordAsync_NoPrescriptions_ReturnsEmptyList()
    {
        List<PrescriptionResponse> result =
            await _prescriptionService.GetByRecordAsync(
                _doctorUserId,
                _recordId);

        Assert.Empty(result);
    }

    // PRES-11
    [Fact(DisplayName = "PRES-11 | Retrieve logged-in patient's prescriptions")]
    public async Task GetMyAsync_ExistingPrescriptions_ReturnsPatientPrescriptions()
    {
        await _prescriptionService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        List<PrescriptionResponse> result =
            await _prescriptionService.GetMyAsync(
                _patientUserId);

        Assert.Single(result);

        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Test Patient", result[0].PatientName);
        Assert.Equal(_doctorId, result[0].DoctorId);
        Assert.Equal("Cetirizine", result[0].Medicine);
    }

    // PRES-12
    [Fact(DisplayName = "PRES-12 | Reject patient without patient profile")]
    public async Task GetMyAsync_MissingPatient_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _prescriptionService.GetMyAsync(Guid.NewGuid()));

        Assert.Equal(
            "Patient profile was not found.",
            exception.Message);
    }

    // PRES-13
    [Fact(DisplayName = "PRES-13 | Retrieve logged-in doctor's prescriptions")]
    public async Task GetMyDoctorPrescriptionsAsync_ExistingPrescriptions_ReturnsDoctorPrescriptions()
    {
        await _prescriptionService.CreateAsync(
            _doctorUserId,
            CreateValidRequest());

        List<PrescriptionResponse> result =
            await _prescriptionService.GetMyDoctorPrescriptionsAsync(
                _doctorUserId);

        Assert.Single(result);

        Assert.Equal(_doctorId, result[0].DoctorId);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Cetirizine", result[0].Medicine);
    }

    // PRES-14
    [Fact(DisplayName = "PRES-14 | Reject doctor without doctor profile when retrieving prescriptions")]
    public async Task GetMyDoctorPrescriptionsAsync_MissingDoctor_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _prescriptionService.GetMyDoctorPrescriptionsAsync(
                    Guid.NewGuid()));

        Assert.Equal(
            "Doctor profile was not found.",
            exception.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}