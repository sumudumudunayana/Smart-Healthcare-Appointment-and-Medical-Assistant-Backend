using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Payments;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class PaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentResponse> CreateAsync(
        Guid userId,
        CreatePaymentRequest request)
    {
        // Find logged-in user
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators and receptionists handle payments
        if (user.RoleId != Guid.Parse(
                "11111111-1111-1111-1111-111111111111") &&
            user.RoleId != Guid.Parse(
                "33333333-3333-3333-3333-333333333333"))
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can process payments.");
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        // Validate payment method
        string paymentMethod = request.PaymentMethod.Trim();

        if (!string.Equals(paymentMethod, "Cash",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(paymentMethod, "Insurance",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Payment method must be Cash or Insurance.");
        }

        // Find the bill
        Bill? bill = await _context.Bills
            .Include(b => b.Patient)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(b =>
                b.BillId == request.BillId);

        if (bill == null)
        {
            throw new ArgumentException(
                "Bill was not found.");
        }

        if (bill.Patient == null ||
            bill.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        // Calculate how much has already been paid
        decimal totalPaid = await _context.Payments
            .Where(p => p.BillId == bill.BillId &&
                        p.PaymentStatus == "Completed")
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        decimal remainingAmount =
            bill.TotalAmount - totalPaid;

        // Prevent payment above bill amount
        if (request.Amount > remainingAmount)
        {
            throw new ArgumentException(
                $"Payment exceeds the remaining bill amount of {remainingAmount:F2}.");
        }

        // Determine payment status
        string paymentStatus =
            request.Amount == remainingAmount
                ? "Completed"
                : "Completed";

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            BillId = bill.BillId,
            Amount = request.Amount,
            PaymentMethod = paymentMethod.Equals(
                "Cash",
                StringComparison.OrdinalIgnoreCase)
                ? "Cash"
                : "Insurance",
            PaymentDate = DateOnly.FromDateTime(
                DateTime.UtcNow),
            PaymentStatus = paymentStatus
        };

        _context.Payments.Add(payment);

        // Update bill status
        decimal newTotalPaid =
            totalPaid + request.Amount;

        if (newTotalPaid >= bill.TotalAmount)
        {
            bill.BillStatus = "Paid";
        }
        else
        {
            bill.BillStatus = "PartiallyPaid";
        }

        await _context.SaveChangesAsync();

        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            BillId = bill.BillId,

            PatientId = bill.PatientId,
            PatientName = bill.Patient.User.FullName,

            BillAmount = bill.TotalAmount,
            PaymentAmount = payment.Amount,
            RemainingAmount = bill.TotalAmount - newTotalPaid,

            PaymentMethod = payment.PaymentMethod,
            PaymentDate = payment.PaymentDate,
            PaymentStatus = payment.PaymentStatus,
            BillStatus = bill.BillStatus
        };
    }

    public async Task<List<PaymentResponse>> GetByBillAsync(
        Guid userId,
        Guid billId)
    {
        // Find logged-in user
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Find bill
        Bill? bill = await _context.Bills
            .Include(b => b.Patient)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(b =>
                b.BillId == billId);

        if (bill == null)
        {
            throw new ArgumentException(
                "Bill was not found.");
        }

        bool isAdministrator =
            user.RoleId == Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

        bool isReceptionist =
            user.RoleId == Guid.Parse(
                "33333333-3333-3333-3333-333333333333");

        bool isPatient =
            user.RoleId == Guid.Parse(
                "44444444-4444-4444-4444-444444444444");

        bool hasAccess =
            isAdministrator ||
            isReceptionist ||
            (isPatient && bill.Patient?.UserId == userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view these payments.");
        }

        if (bill.Patient == null ||
            bill.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        decimal totalPaid = await _context.Payments
            .Where(p => p.BillId == billId &&
                        p.PaymentStatus == "Completed")
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.BillId == billId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentResponse
            {
                PaymentId = p.PaymentId,
                BillId = p.BillId,

                PatientId = bill.PatientId,
                PatientName = bill.Patient!.User!.FullName,

                BillAmount = bill.TotalAmount,
                PaymentAmount = p.Amount,
                RemainingAmount =
                    bill.TotalAmount - totalPaid,

                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate,
                PaymentStatus = p.PaymentStatus,
                BillStatus = bill.BillStatus
            })
            .ToListAsync();
    }


    public async Task<List<PaymentResponse>> GetAllForReceptionistAsync()
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.Bill)
                .ThenInclude(b => b!.Patient)
                    .ThenInclude(p => p!.User)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentResponse
            {
                PaymentId = p.PaymentId,
                BillId = p.BillId,

                PatientId = p.Bill!.PatientId,
                PatientName = p.Bill.Patient!.User!.FullName,

                BillAmount = p.Bill.TotalAmount,
                PaymentAmount = p.Amount,

                RemainingAmount =
                    p.Bill.TotalAmount -
                    _context.Payments
                        .Where(x =>
                            x.BillId == p.BillId &&
                            x.PaymentStatus == "Completed")
                        .Sum(x => (decimal?)x.Amount) ?? 0,

                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate,
                PaymentStatus = p.PaymentStatus,
                BillStatus = p.Bill.BillStatus
            })
            .ToListAsync();
    }
}