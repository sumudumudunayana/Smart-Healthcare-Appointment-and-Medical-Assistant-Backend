using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Payments;
using SmartHealthcare.API.Models;
using SmartHealthcare.API.Services;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class PaymentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentService _paymentService;

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
    private readonly Guid _otherPatientUserId = Guid.NewGuid();
    private readonly Guid _doctorUserId = Guid.NewGuid();

    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _billId = Guid.NewGuid();

    private const decimal BillAmount = 2500.00m;

    public PaymentServiceTests()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        _context = new ApplicationDbContext(options);
        _paymentService = new PaymentService(_context);

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

        var otherPatientUser = new User
        {
            UserId = _otherPatientUserId,
            FullName = "Another Patient",
            Email = "anotherpatient@example.com",
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

        var bill = new Bill
        {
            BillId = _billId,
            BillNumber = "BILL-000001",
            AppointmentId = Guid.NewGuid(),
            PatientId = _patientId,
            TotalAmount = BillAmount,
            BillStatus = "Pending",
            GeneratedDate =
                DateOnly.FromDateTime(DateTime.UtcNow),
            Patient = patient
        };

        _context.Users.AddRange(
            adminUser,
            receptionistUser,
            patientUser,
            otherPatientUser,
            doctorUser);

        _context.Patients.Add(patient);
        _context.Bills.Add(bill);

        _context.SaveChanges();
    }

    private CreatePaymentRequest CreateValidRequest(
        decimal amount = BillAmount,
        string paymentMethod = "Cash",
        Guid? billId = null)
    {
        return new CreatePaymentRequest
        {
            BillId = billId ?? _billId,
            Amount = amount,
            PaymentMethod = paymentMethod
        };
    }

    private async Task<PaymentResponse> CreatePaymentAsync(
        decimal amount = BillAmount,
        string paymentMethod = "Cash",
        Guid? userId = null,
        Guid? billId = null)
    {
        return await _paymentService.CreateAsync(
            userId ?? _adminUserId,
            CreateValidRequest(
                amount,
                paymentMethod,
                billId));
    }

    private async Task AddExistingPaymentAsync(
        decimal amount,
        string status = "Completed",
        string paymentMethod = "Cash")
    {
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            BillId = _billId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentDate =
                DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentStatus = status
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
    }

    // PAY-01
    [Fact(DisplayName = "PAY-01 | Create a full payment successfully")]
    public async Task CreateAsync_FullPayment_CreatesPayment()
    {
        PaymentResponse response =
            await CreatePaymentAsync();

        Assert.NotEqual(Guid.Empty, response.PaymentId);
        Assert.Equal(_billId, response.BillId);
        Assert.Equal(_patientId, response.PatientId);
        Assert.Equal("Test Patient", response.PatientName);
        Assert.Equal(BillAmount, response.BillAmount);
        Assert.Equal(BillAmount, response.PaymentAmount);
        Assert.Equal(0m, response.RemainingAmount);
        Assert.Equal("Cash", response.PaymentMethod);
        Assert.Equal("Completed", response.PaymentStatus);
        Assert.Equal("Paid", response.BillStatus);

        Assert.Equal(
            "Paid",
            (await _context.Bills.FindAsync(_billId))!.BillStatus);

        Assert.Single(await _context.Payments.ToListAsync());
    }

    // PAY-02
    [Fact(DisplayName = "PAY-02 | Create a partial payment successfully")]
    public async Task CreateAsync_PartialPayment_CreatesPayment()
    {
        PaymentResponse response =
            await CreatePaymentAsync(amount: 1000m);

        Assert.Equal(1000m, response.PaymentAmount);
        Assert.Equal(1500m, response.RemainingAmount);
        Assert.Equal("Completed", response.PaymentStatus);
        Assert.Equal("PartiallyPaid", response.BillStatus);

        Assert.Equal(
            "PartiallyPaid",
            (await _context.Bills.FindAsync(_billId))!.BillStatus);
    }

    // PAY-03
    [Fact(DisplayName = "PAY-03 | Reject zero payment amount")]
    public async Task CreateAsync_ZeroAmount_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(amount: 0m));

        Assert.Equal(
            "Payment amount must be greater than zero.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-04
    [Fact(DisplayName = "PAY-04 | Reject negative payment amount")]
    public async Task CreateAsync_NegativeAmount_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(amount: -100m));

        Assert.Equal(
            "Payment amount must be greater than zero.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-05
    [Fact(DisplayName = "PAY-05 | Reject invalid payment method")]
    public async Task CreateAsync_InvalidPaymentMethod_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(
                    amount: 500m,
                    paymentMethod: "CreditCard"));

        Assert.Equal(
            "Payment method must be Cash or Insurance.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-06
    [Fact(DisplayName = "PAY-06 | Reject payment for nonexistent bill")]
    public async Task CreateAsync_MissingBill_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(
                    billId: Guid.NewGuid()));

        Assert.Equal(
            "Bill was not found.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-07
    [Fact(DisplayName = "PAY-07 | Reject payment exceeding bill amount")]
    public async Task CreateAsync_ExceedsBillAmount_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(
                    amount: BillAmount + 1m));

        Assert.Equal(
            "Payment exceeds the remaining bill amount of 2500.00.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-08
    [Fact(DisplayName = "PAY-08 | Reject payment exceeding remaining balance")]
    public async Task CreateAsync_ExceedsRemainingBalance_ThrowsArgumentException()
    {
        await AddExistingPaymentAsync(1000m);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreatePaymentAsync(amount: 1500.01m));

        Assert.Equal(
            "Payment exceeds the remaining bill amount of 1500.00.",
            exception.Message);

        Assert.Single(await _context.Payments.ToListAsync());
    }

    // PAY-09
    [Fact(DisplayName = "PAY-09 | Update bill status to PartiallyPaid")]
    public async Task CreateAsync_PartialPayment_UpdatesBillStatus()
    {
        await CreatePaymentAsync(amount: 500m);

        Bill bill =
            (await _context.Bills.FindAsync(_billId))!;

        Assert.Equal("PartiallyPaid", bill.BillStatus);
    }

    // PAY-10
    [Fact(DisplayName = "PAY-10 | Update bill status to Paid")]
    public async Task CreateAsync_FullPayment_UpdatesBillStatus()
    {
        await CreatePaymentAsync(amount: 1500m);

        await CreatePaymentAsync(amount: 1000m);

        Bill bill =
            (await _context.Bills.FindAsync(_billId))!;

        Assert.Equal("Paid", bill.BillStatus);
        Assert.Equal(
            2,
            await _context.Payments.CountAsync());
    }

    // PAY-11
    [Fact(DisplayName = "PAY-11 | Reject payment creation by a patient")]
    public async Task CreateAsync_PatientUser_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreatePaymentAsync(
                    amount: 500m,
                    userId: _patientUserId));

        Assert.Equal(
            "Only administrators or receptionists can process payments.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-12
    [Fact(DisplayName = "PAY-12 | Reject payment creation by nonexistent user")]
    public async Task CreateAsync_MissingUser_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreatePaymentAsync(
                    userId: Guid.NewGuid()));

        Assert.Equal(
            "User was not found.",
            exception.Message);

        Assert.Empty(await _context.Payments.ToListAsync());
    }

    // PAY-13
    [Fact(DisplayName = "PAY-13 | Normalize payment method")]
    public async Task CreateAsync_LowercasePaymentMethod_NormalizesMethod()
    {
        PaymentResponse response =
            await CreatePaymentAsync(
                amount: 500m,
                paymentMethod: "cash");

        Assert.Equal("Cash", response.PaymentMethod);

        Payment payment =
            (await _context.Payments.SingleAsync());

        Assert.Equal("Cash", payment.PaymentMethod);
    }

    // PAY-14
    [Fact(DisplayName = "PAY-14 | Retrieve payments for a bill")]
    public async Task GetByBillAsync_ExistingPayments_ReturnsPayments()
    {
        await AddExistingPaymentAsync(500m);
        await AddExistingPaymentAsync(1000m);

        List<PaymentResponse> result =
            await _paymentService.GetByBillAsync(
                _adminUserId,
                _billId);

        Assert.Equal(2, result.Count);

        Assert.All(
            result,
            payment =>
            {
                Assert.Equal(_billId, payment.BillId);
                Assert.Equal(_patientId, payment.PatientId);
                Assert.Equal("Test Patient", payment.PatientName);
                Assert.Equal(BillAmount, payment.BillAmount);
            });
    }

    // PAY-15
    [Fact(DisplayName = "PAY-15 | Allow patient to view their own payments")]
    public async Task GetByBillAsync_OwningPatient_ReturnsPayments()
    {
        await AddExistingPaymentAsync(500m);

        List<PaymentResponse> result =
            await _paymentService.GetByBillAsync(
                _patientUserId,
                _billId);

        Assert.Single(result);
        Assert.Equal(_billId, result[0].BillId);
        Assert.Equal(_patientId, result[0].PatientId);
        Assert.Equal("Test Patient", result[0].PatientName);
    }

    // PAY-16
    [Fact(DisplayName = "PAY-16 | Prevent patient viewing another patient's payments")]
    public async Task GetByBillAsync_OtherPatient_ThrowsUnauthorizedAccessException()
    {
        await AddExistingPaymentAsync(500m);

        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _paymentService.GetByBillAsync(
                    _otherPatientUserId,
                    _billId));

        Assert.Equal(
            "You do not have permission to view these payments.",
            exception.Message);
    }

    // PAY-17
    [Fact(DisplayName = "PAY-17 | Reject payment history request for nonexistent bill")]
    public async Task GetByBillAsync_MissingBill_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _paymentService.GetByBillAsync(
                    _adminUserId,
                    Guid.NewGuid()));

        Assert.Equal(
            "Bill was not found.",
            exception.Message);
    }

    // PAY-18
    [Fact(DisplayName = "PAY-18 | Reject payment history request by nonexistent user")]
    public async Task GetByBillAsync_MissingUser_ThrowsInvalidOperationException()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.GetByBillAsync(
                    Guid.NewGuid(),
                    _billId));

        Assert.Equal(
            "User was not found.",
            exception.Message);
    }

    // PAY-19
    [Fact(DisplayName = "PAY-19 | Retrieve all payments for receptionist")]
    public async Task GetAllForReceptionistAsync_ExistingPayments_ReturnsPayments()
    {
        await AddExistingPaymentAsync(500m);
        await AddExistingPaymentAsync(
            1000m,
            paymentMethod: "Insurance");

        List<PaymentResponse> result =
            await _paymentService.GetAllForReceptionistAsync();

        Assert.Equal(2, result.Count);

        Assert.All(
            result,
            payment =>
            {
                Assert.Equal(_billId, payment.BillId);
                Assert.Equal(_patientId, payment.PatientId);
                Assert.Equal("Test Patient", payment.PatientName);
                Assert.Equal(BillAmount, payment.BillAmount);
            });
    }

    // PAY-20
    [Fact(DisplayName = "PAY-20 | Allow receptionist to process payment")]
    public async Task CreateAsync_Receptionist_CreatesPayment()
    {
        PaymentResponse response =
            await CreatePaymentAsync(
                amount: 500m,
                userId: _receptionistUserId);

        Assert.Equal(500m, response.PaymentAmount);
        Assert.Equal(2000m, response.RemainingAmount);
        Assert.Equal("PartiallyPaid", response.BillStatus);

        Assert.Single(await _context.Payments.ToListAsync());
    }

    // PAY-21
    [Fact(DisplayName = "PAY-21 | Allow Insurance payment method")]
    public async Task CreateAsync_InsurancePayment_CreatesPayment()
    {
        PaymentResponse response =
            await CreatePaymentAsync(
                amount: 500m,
                paymentMethod: "insurance");

        Assert.Equal("Insurance", response.PaymentMethod);
        Assert.Equal("Completed", response.PaymentStatus);

        Payment payment =
            await _context.Payments.SingleAsync();

        Assert.Equal("Insurance", payment.PaymentMethod);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}