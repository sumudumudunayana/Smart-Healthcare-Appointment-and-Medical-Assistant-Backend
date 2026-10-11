using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Bills;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class BillServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BillService _billService;

    private static readonly Guid AdministratorRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid DoctorRoleId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ReceptionistRoleId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid PatientRoleId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _receptionistUserId = Guid.NewGuid();
    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();

    private readonly Guid _appointmentId = Guid.NewGuid();

    public BillServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _billService = new BillService(_context);

        SeedData();
    }

    private void SeedData()
    {
        var adminUser = new User
        {
            UserId = _adminUserId,
            FullName = "Test Administrator",
            Email = "admin@example.com",
            PasswordHash = "test-hash",
            RoleId = AdministratorRoleId,
            Status = "Active"
        };

        var receptionistUser = new User
        {
            UserId = _receptionistUserId,
            FullName = "Test Receptionist",
            Email = "receptionist@example.com",
            PasswordHash = "test-hash",
            RoleId = ReceptionistRoleId,
            Status = "Active"
        };

        var patientUser = new User
        {
            UserId = _patientUserId,
            FullName = "Test Patient",
            Email = "patient@example.com",
            PasswordHash = "test-hash",
            RoleId = PatientRoleId,
            Status = "Active"
        };

        var doctorUser = new User
        {
            UserId = _doctorUserId,
            FullName = "Dr. Test Doctor",
            Email = "doctor@example.com",
            PasswordHash = "test-hash",
            RoleId = DoctorRoleId,
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
            LicenseNumber = "BILL-TEST-DOCTOR-001",
            Experience = 5
        };

        var appointment = new Appointment
        {
            AppointmentId = _appointmentId,
            PatientId = _patientId,
            DoctorId = _doctorId,
            ScheduleId = Guid.NewGuid(),
            AppointmentDate =
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            AppointmentTime = new TimeOnly(10, 0),
            Status = "Completed",
            Symptoms = "General checkup",
            Patient = patient,
            Doctor = doctor
        };

        _context.Users.AddRange(
            adminUser,
            receptionistUser,
            patientUser,
            doctorUser);

        _context.Patients.Add(patient);
        _context.Doctors.Add(doctor);
        _context.Appointments.Add(appointment);

        _context.SaveChanges();
    }

    private CreateBillRequest CreateValidRequest()
    {
        return new CreateBillRequest
        {
            AppointmentId = _appointmentId,
            TotalAmount = 2500.00m
        };
    }

    private async Task<Guid> AddAnotherAppointmentAsync()
    {
        var appointment = new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = _doctorId,
            ScheduleId = Guid.NewGuid(),
            AppointmentDate =
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14),
            AppointmentTime = new TimeOnly(11, 0),
            Status = "Completed"
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return appointment.AppointmentId;
    }

    private async Task<BillResponse> CreateBillAsync(
        Guid? appointmentId = null,
        decimal amount = 2500.00m)
    {
        var request = new CreateBillRequest
        {
            AppointmentId = appointmentId ?? _appointmentId,
            TotalAmount = amount
        };

        return await _billService.CreateAsync(
            _adminUserId,
            request);
    }

    // BILL-01
    [Fact(DisplayName = "BILL-01 | Create bill with valid details")]
    public async Task CreateAsync_ValidRequest_CreatesBill()
    {
        BillResponse response = await _billService.CreateAsync(
            _adminUserId,
            CreateValidRequest());

        Assert.NotEqual(Guid.Empty, response.BillId);
        Assert.Equal("BILL-000001", response.BillNumber);
        Assert.Equal(_appointmentId, response.AppointmentId);
        Assert.Equal(_patientId, response.PatientId);
        Assert.Equal("Test Patient", response.PatientName);
        Assert.Equal(_doctorId, response.DoctorId);
        Assert.Equal("Dr. Test Doctor", response.DoctorName);
        Assert.Equal(2500.00m, response.TotalAmount);
        Assert.Equal("Pending", response.BillStatus);

        Assert.Equal(
            1,
            await _context.Bills.CountAsync());
    }

    // BILL-02
    [Fact(DisplayName = "BILL-02 | Reject negative bill amount")]
    public async Task CreateAsync_NegativeAmount_ThrowsArgumentException()
    {
        CreateBillRequest request = CreateValidRequest();
        request.TotalAmount = -100m;

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _billService.CreateAsync(
                _adminUserId,
                request));

        Assert.Equal(
            "Bill amount cannot be negative.",
            exception.Message);

        Assert.Empty(await _context.Bills.ToListAsync());
    }

    // BILL-03
    [Fact(DisplayName = "BILL-03 | Reject nonexistent appointment")]
    public async Task CreateAsync_MissingAppointment_ThrowsArgumentException()
    {
        CreateBillRequest request = CreateValidRequest();
        request.AppointmentId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _billService.CreateAsync(
                _adminUserId,
                request));

        Assert.Equal(
            "Appointment was not found.",
            exception.Message);

        Assert.Empty(await _context.Bills.ToListAsync());
    }

    // BILL-04
    [Fact(DisplayName = "BILL-04 | Prevent duplicate bills for an appointment")]
    public async Task CreateAsync_DuplicateBill_ThrowsInvalidOperationException()
    {
        await CreateBillAsync();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateBillAsync());

        Assert.Equal(
            "A bill already exists for this appointment.",
            exception.Message);

        Assert.Equal(
            1,
            await _context.Bills.CountAsync());
    }

    // BILL-05
    [Fact(DisplayName = "BILL-05 | Reject unauthorized bill creation")]
    public async Task CreateAsync_UnauthorizedUser_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _billService.CreateAsync(
                    _doctorUserId,
                    CreateValidRequest()));

        Assert.Equal(
            "Only administrators or receptionists can create bills.",
            exception.Message);

        Assert.Empty(await _context.Bills.ToListAsync());
    }

    // BILL-06
    [Fact(DisplayName = "BILL-06 | Generate sequential bill numbers")]
    public async Task CreateAsync_MultipleBills_GeneratesSequentialNumbers()
    {
        BillResponse firstBill = await CreateBillAsync();

        Guid secondAppointmentId =
            await AddAnotherAppointmentAsync();

        BillResponse secondBill = await CreateBillAsync(
            secondAppointmentId);

        Assert.Equal("BILL-000001", firstBill.BillNumber);
        Assert.Equal("BILL-000002", secondBill.BillNumber);

        Assert.NotEqual(
            firstBill.BillNumber,
            secondBill.BillNumber);
    }

    // BILL-07
    [Fact(DisplayName = "BILL-07 | Retrieve bill by ID")]
    public async Task GetByIdAsync_ExistingBill_ReturnsBill()
    {
        BillResponse createdBill = await CreateBillAsync();

        BillResponse? result = await _billService.GetByIdAsync(
            _patientUserId,
            createdBill.BillId);

        Assert.NotNull(result);
        Assert.Equal(createdBill.BillId, result.BillId);
        Assert.Equal(createdBill.BillNumber, result.BillNumber);
        Assert.Equal(_patientId, result.PatientId);
        Assert.Equal("Test Patient", result.PatientName);
        Assert.Equal("Dr. Test Doctor", result.DoctorName);
        Assert.Equal(2500.00m, result.TotalAmount);
    }

    // BILL-08
    [Fact(DisplayName = "BILL-08 | Return null for nonexistent bill")]
    public async Task GetByIdAsync_MissingBill_ReturnsNull()
    {
        BillResponse? result = await _billService.GetByIdAsync(
            _patientUserId,
            Guid.NewGuid());

        Assert.Null(result);
    }

    // BILL-09
    [Fact(DisplayName = "BILL-09 | Prevent another patient from viewing a bill")]
    public async Task GetByIdAsync_OtherPatient_ThrowsUnauthorizedAccessException()
    {
        BillResponse bill = await CreateBillAsync();

        var otherPatientUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Another Patient",
            Email = "anotherpatient@example.com",
            PasswordHash = "test-hash",
            RoleId = PatientRoleId,
            Status = "Active"
        };

        _context.Users.Add(otherPatientUser);
        await _context.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _billService.GetByIdAsync(
                    otherPatientUser.UserId,
                    bill.BillId));

        Assert.Equal(
            "You do not have permission to view this bill.",
            exception.Message);
    }

    // BILL-10
    [Fact(DisplayName = "BILL-10 | Retrieve logged-in patient's bills")]
    public async Task GetMyAsync_ExistingBills_ReturnsPatientBills()
    {
        await CreateBillAsync();

        List<BillResponse> result =
            await _billService.GetMyAsync(_patientUserId);

        Assert.Single(result);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Test Patient", result[0].PatientName);
        Assert.Equal("Dr. Test Doctor", result[0].DoctorName);
        Assert.Equal(2500.00m, result[0].TotalAmount);
    }

    // BILL-11
    [Fact(DisplayName = "BILL-11 | Reject user without patient profile")]
    public async Task GetMyAsync_MissingPatient_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _billService.GetMyAsync(_adminUserId));

        Assert.Equal(
            "Patient profile was not found.",
            exception.Message);
    }

    // BILL-12
    [Fact(DisplayName = "BILL-12 | Retrieve all bills for receptionist")]
    public async Task GetAllForReceptionistAsync_ExistingBills_ReturnsAllBills()
    {
        await CreateBillAsync();

        Guid secondAppointmentId =
            await AddAnotherAppointmentAsync();

        await CreateBillAsync(secondAppointmentId, 1500.00m);

        List<BillResponse> result =
            await _billService.GetAllForReceptionistAsync();

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            bill => bill.TotalAmount == 2500.00m);

        Assert.Contains(
            result,
            bill => bill.TotalAmount == 1500.00m);
    }

    // BILL-13
    [Fact(DisplayName = "BILL-13 | Allow receptionist to create a bill")]
    public async Task CreateAsync_Receptionist_CreatesBill()
    {
        BillResponse response = await _billService.CreateAsync(
            _receptionistUserId,
            CreateValidRequest());

        Assert.Equal(_appointmentId, response.AppointmentId);
        Assert.Equal("Pending", response.BillStatus);
        Assert.Equal(2500.00m, response.TotalAmount);

        Assert.Single(await _context.Bills.ToListAsync());
    }

    // BILL-14
    [Fact(DisplayName = "BILL-14 | Allow zero-value bill")]
    public async Task CreateAsync_ZeroAmount_CreatesBill()
    {
        BillResponse response = await CreateBillAsync(amount: 0m);

        Assert.Equal(0m, response.TotalAmount);
        Assert.Equal("Pending", response.BillStatus);

        Assert.Single(await _context.Bills.ToListAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}