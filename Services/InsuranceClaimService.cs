using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.InsuranceClaims;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class InsuranceClaimService
{
    private readonly ApplicationDbContext _context;

    public InsuranceClaimService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InsuranceClaimResponse> CreateAsync(
        Guid userId,
        CreateInsuranceClaimRequest request)
    {
        // Find logged-in user
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators and receptionists process insurance claims
        if (user.RoleId != Guid.Parse(
                "11111111-1111-1111-1111-111111111111") &&
            user.RoleId != Guid.Parse(
                "33333333-3333-3333-3333-333333333333"))
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can process insurance claims.");
        }

        // Validate claim amount
        if (request.ClaimAmount <= 0)
        {
            throw new ArgumentException(
                "Claim amount must be greater than zero.");
        }

        // Find the bill
        Bill? bill = await _context.Bills
            .Include(b => b.Patient)
                .ThenInclude(p => p!.User)
            .Include(b => b.Appointment)
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

        // Claim cannot exceed the bill amount
        if (request.ClaimAmount > bill.TotalAmount)
        {
            throw new ArgumentException(
                "Claim amount cannot exceed the bill amount.");
        }

        // Find insurance policy
        InsurancePolicy? policy = await _context.InsurancePolicies
            .FirstOrDefaultAsync(i =>
                i.InsurancePolicyId == request.InsurancePolicyId);

        if (policy == null)
        {
            throw new ArgumentException(
                "Insurance policy was not found.");
        }

        // Policy must belong to the patient
        if (policy.PatientId != bill.PatientId)
        {
            throw new UnauthorizedAccessException(
                "The insurance policy does not belong to this patient.");
        }

        // Policy must be active
        if (!string.Equals(
                policy.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The insurance policy is not active.");
        }

        // Check policy date against appointment date
        if (bill.Appointment == null)
        {
            throw new InvalidOperationException(
                "Appointment information was not found.");
        }

        DateOnly serviceDate =
            bill.Appointment.AppointmentDate;

        if (serviceDate < policy.StartDate ||
            serviceDate > policy.EndDate)
        {
            throw new InvalidOperationException(
                "The insurance policy was not valid on the appointment date.");
        }

        // Claim cannot exceed available coverage
        if (request.ClaimAmount > policy.CoverageAmount)
        {
            throw new ArgumentException(
                "Claim amount cannot exceed the insurance coverage amount.");
        }

        // Prevent duplicate claim for the same bill and policy
        bool claimExists = await _context.InsuranceClaims
            .AnyAsync(c =>
                c.BillId == bill.BillId &&
                c.InsurancePolicyId == policy.InsurancePolicyId);

        if (claimExists)
        {
            throw new InvalidOperationException(
                "An insurance claim already exists for this bill and policy.");
        }

        var claim = new InsuranceClaim
        {
            ClaimId = Guid.NewGuid(),
            InsurancePolicyId = policy.InsurancePolicyId,
            BillId = bill.BillId,
            ClaimAmount = request.ClaimAmount,
            Status = "Pending",
            ClaimDetails = request.ClaimDetails?.Trim(),
            SubmittedAt = DateTime.UtcNow
        };

        _context.InsuranceClaims.Add(claim);

        await _context.SaveChangesAsync();

        return new InsuranceClaimResponse
        {
            ClaimId = claim.ClaimId,

            InsurancePolicyId = policy.InsurancePolicyId,
            ProviderName = policy.ProviderName,
            PolicyNumber = policy.PolicyNumber,

            BillId = bill.BillId,
            PatientId = bill.PatientId,
            PatientName = bill.Patient.User.FullName,

            BillAmount = bill.TotalAmount,
            ClaimAmount = claim.ClaimAmount,

            Status = claim.Status,
            ClaimDetails = claim.ClaimDetails,

            SubmittedAt = claim.SubmittedAt,
            ProcessedAt = claim.ProcessedAt
        };
    }

    public async Task<List<InsuranceClaimResponse>> GetByBillAsync(
        Guid userId,
        Guid billId)
    {
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

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
                "You do not have permission to view these insurance claims.");
        }

        return await _context.InsuranceClaims
            .AsNoTracking()
            .Where(c => c.BillId == billId)
            .Include(c => c.InsurancePolicy)
            .Include(c => c.Bill)
                .ThenInclude(b => b!.Patient)
                    .ThenInclude(p => p!.User)
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new InsuranceClaimResponse
            {
                ClaimId = c.ClaimId,

                InsurancePolicyId = c.InsurancePolicyId,
                ProviderName = c.InsurancePolicy!.ProviderName,
                PolicyNumber = c.InsurancePolicy.PolicyNumber,

                BillId = c.BillId,
                PatientId = c.Bill!.PatientId,
                PatientName = c.Bill.Patient!.User!.FullName,

                BillAmount = c.Bill.TotalAmount,
                ClaimAmount = c.ClaimAmount,

                Status = c.Status,
                ClaimDetails = c.ClaimDetails,

                SubmittedAt = c.SubmittedAt,
                ProcessedAt = c.ProcessedAt
            })
            .ToListAsync();
    }

    public async Task<List<InsuranceClaimResponse>> GetMyAsync(
        Guid userId)
    {
        Patient? patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "Patient profile was not found.");
        }

        return await _context.InsuranceClaims
            .AsNoTracking()
            .Where(c =>
                c.Bill!.PatientId == patient.PatientId)
            .Include(c => c.InsurancePolicy)
            .Include(c => c.Bill)
                .ThenInclude(b => b!.Patient)
                    .ThenInclude(p => p!.User)
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new InsuranceClaimResponse
            {
                ClaimId = c.ClaimId,

                InsurancePolicyId = c.InsurancePolicyId,
                ProviderName = c.InsurancePolicy!.ProviderName,
                PolicyNumber = c.InsurancePolicy.PolicyNumber,

                BillId = c.BillId,
                PatientId = c.Bill!.PatientId,
                PatientName = c.Bill.Patient!.User!.FullName,

                BillAmount = c.Bill.TotalAmount,
                ClaimAmount = c.ClaimAmount,

                Status = c.Status,
                ClaimDetails = c.ClaimDetails,

                SubmittedAt = c.SubmittedAt,
                ProcessedAt = c.ProcessedAt
            })
            .ToListAsync();
    }
}