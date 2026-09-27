using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Bills;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class BillService
{
    private readonly ApplicationDbContext _context;

    public BillService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BillResponse> CreateAsync(
        Guid userId,
        CreateBillRequest request)
    {
        // Find the logged-in user
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators/receptionists should create bills
        if (user.RoleId != Guid.Parse("11111111-1111-1111-1111-111111111111") &&
            user.RoleId != Guid.Parse("33333333-3333-3333-3333-333333333333"))
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can create bills.");
        }

        // Validate amount
        if (request.TotalAmount < 0)
        {
            throw new ArgumentException(
                "Bill amount cannot be negative.");
        }

        // Find the appointment
        Appointment? appointment = await _context.Appointments
            .Include(a => a.Patient)
                .ThenInclude(p => p!.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == request.AppointmentId);

        if (appointment == null)
        {
            throw new ArgumentException(
                "Appointment was not found.");
        }

        if (appointment.Patient == null ||
            appointment.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        if (appointment.Doctor == null ||
            appointment.Doctor.User == null)
        {
            throw new InvalidOperationException(
                "Doctor information was not found.");
        }

        // One appointment can only have one bill
        bool billExists = await _context.Bills
            .AnyAsync(b =>
                b.AppointmentId == request.AppointmentId);

        if (billExists)
        {
            throw new InvalidOperationException(
                "A bill already exists for this appointment.");
        }

        var bill = new Bill
        {
            BillId = Guid.NewGuid(),
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            TotalAmount = request.TotalAmount,
            BillStatus = "Pending",
            GeneratedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _context.Bills.Add(bill);

        await _context.SaveChangesAsync();

        return new BillResponse
        {
            BillId = bill.BillId,
            AppointmentId = bill.AppointmentId,

            PatientId = bill.PatientId,
            PatientName = appointment.Patient.User.FullName,

            DoctorId = appointment.DoctorId,
            DoctorName = appointment.Doctor.User.FullName,

            TotalAmount = bill.TotalAmount,
            BillStatus = bill.BillStatus,
            GeneratedDate = bill.GeneratedDate
        };
    }

    public async Task<BillResponse?> GetByIdAsync(
        Guid userId,
        Guid billId)
    {
        Bill? bill = await _context.Bills
            .AsNoTracking()
            .Include(b => b.Patient)
                .ThenInclude(p => p!.User)
            .Include(b => b.Appointment)
                .ThenInclude(a => a!.Doctor)
                    .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(b =>
                b.BillId == billId);

        if (bill == null)
        {
            return null;
        }

        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
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

        bool isDoctor =
            user.RoleId == Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

        bool hasAccess =
            isAdministrator ||
            isReceptionist ||
            (isPatient && bill.Patient?.UserId == userId) ||
            (isDoctor &&
             bill.Appointment?.Doctor?.UserId == userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view this bill.");
        }

        if (bill.Patient == null ||
            bill.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        if (bill.Appointment == null ||
            bill.Appointment.Doctor == null ||
            bill.Appointment.Doctor.User == null)
        {
            throw new InvalidOperationException(
                "Doctor information was not found.");
        }

        return new BillResponse
        {
            BillId = bill.BillId,
            AppointmentId = bill.AppointmentId,

            PatientId = bill.PatientId,
            PatientName = bill.Patient.User.FullName,

            DoctorId = bill.Appointment.DoctorId,
            DoctorName = bill.Appointment.Doctor.User.FullName,

            TotalAmount = bill.TotalAmount,
            BillStatus = bill.BillStatus,
            GeneratedDate = bill.GeneratedDate
        };
    }

    public async Task<List<BillResponse>> GetMyAsync(
        Guid userId)
    {
        Patient? patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "Patient profile was not found.");
        }

        return await _context.Bills
            .AsNoTracking()
            .Include(b => b.Patient)
                .ThenInclude(p => p!.User)
            .Include(b => b.Appointment)
                .ThenInclude(a => a!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(b => b.PatientId == patient.PatientId)
            .OrderByDescending(b => b.GeneratedDate)
            .Select(b => new BillResponse
            {
                BillId = b.BillId,
                AppointmentId = b.AppointmentId,

                PatientId = b.PatientId,
                PatientName = b.Patient!.User!.FullName,

                DoctorId = b.Appointment!.DoctorId,
                DoctorName = b.Appointment.Doctor!.User!.FullName,

                TotalAmount = b.TotalAmount,
                BillStatus = b.BillStatus,
                GeneratedDate = b.GeneratedDate
            })
            .ToListAsync();
    }
}