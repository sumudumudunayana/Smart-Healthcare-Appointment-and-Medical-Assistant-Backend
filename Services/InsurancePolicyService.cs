using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.InsurancePolicies;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class InsurancePolicyService
{
    private readonly ApplicationDbContext _context;

    public InsurancePolicyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InsurancePolicyResponse> CreateAsync(
        Guid userId,
        CreateInsurancePolicyRequest request)
    {
        // Find logged-in user
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User was not found.");
        }

        // Only administrators and receptionists can create policies
        if (user.RoleId != Guid.Parse(
                "11111111-1111-1111-1111-111111111111") &&
            user.RoleId != Guid.Parse(
                "33333333-3333-3333-3333-333333333333"))
        {
            throw new InvalidOperationException(
                "Only administrators or receptionists can create insurance policies.");
        }

        // Validate provider
        string providerName = request.ProviderName.Trim();

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException(
                "Insurance provider name is required.");
        }

        // Validate policy number
        string policyNumber = request.PolicyNumber.Trim();

        if (string.IsNullOrWhiteSpace(policyNumber))
        {
            throw new ArgumentException(
                "Policy number is required.");
        }

        // Validate dates
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException(
                "Insurance policy end date cannot be before the start date.");
        }

        // Validate coverage
        if (request.CoverageAmount < 0)
        {
            throw new ArgumentException(
                "Coverage amount cannot be negative.");
        }

        // Find patient
        Patient? patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p =>
                p.PatientId == request.PatientId);

        if (patient == null)
        {
            throw new ArgumentException(
                "Patient was not found.");
        }

        if (patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient user information was not found.");
        }

        // Policy number must be unique
        bool policyExists = await _context.InsurancePolicies
            .AnyAsync(i =>
                i.PolicyNumber == policyNumber);

        if (policyExists)
        {
            throw new InvalidOperationException(
                "An insurance policy with this policy number already exists.");
        }

        var policy = new InsurancePolicy
        {
            InsurancePolicyId = Guid.NewGuid(),
            PatientId = patient.PatientId,
            ProviderName = providerName,
            PolicyNumber = policyNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CoverageAmount = request.CoverageAmount,
            Status = "Active"
        };

        _context.InsurancePolicies.Add(policy);

        await _context.SaveChangesAsync();

        return new InsurancePolicyResponse
        {
            InsurancePolicyId = policy.InsurancePolicyId,
            PatientId = patient.PatientId,
            PatientName = patient.User.FullName,

            ProviderName = policy.ProviderName,
            PolicyNumber = policy.PolicyNumber,

            StartDate = policy.StartDate,
            EndDate = policy.EndDate,

            CoverageAmount = policy.CoverageAmount,
            Status = policy.Status
        };
    }

    public async Task<List<InsurancePolicyResponse>> GetMyAsync(
        Guid userId)
    {
        Patient? patient = await _context.Patients
            .FirstOrDefaultAsync(p =>
                p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "Patient profile was not found.");
        }

        return await _context.InsurancePolicies
            .AsNoTracking()
            .Where(i => i.PatientId == patient.PatientId)
            .Include(i => i.Patient)
                .ThenInclude(p => p!.User)
            .OrderByDescending(i => i.StartDate)
            .Select(i => new InsurancePolicyResponse
            {
                InsurancePolicyId = i.InsurancePolicyId,
                PatientId = i.PatientId,
                PatientName = i.Patient!.User!.FullName,

                ProviderName = i.ProviderName,
                PolicyNumber = i.PolicyNumber,

                StartDate = i.StartDate,
                EndDate = i.EndDate,

                CoverageAmount = i.CoverageAmount,
                Status = i.Status
            })
            .ToListAsync();
    }

    public async Task<List<InsurancePolicyResponse>> GetAllAsync(
        Guid userId)
    {
        User? user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == userId);

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

        if (!isAdministrator && !isReceptionist)
        {
            throw new UnauthorizedAccessException(
                "Only administrators or receptionists can view all insurance policies.");
        }

        return await _context.InsurancePolicies
            .AsNoTracking()
            .Include(i => i.Patient)
                .ThenInclude(p => p!.User)
            .OrderByDescending(i => i.StartDate)
            .Select(i => new InsurancePolicyResponse
            {
                InsurancePolicyId = i.InsurancePolicyId,
                PatientId = i.PatientId,
                PatientName = i.Patient!.User!.FullName,

                ProviderName = i.ProviderName,
                PolicyNumber = i.PolicyNumber,

                StartDate = i.StartDate,
                EndDate = i.EndDate,

                CoverageAmount = i.CoverageAmount,
                Status = i.Status
            })
            .ToListAsync();
    }

    public async Task<InsurancePolicyResponse?> GetByIdAsync(
        Guid userId,
        Guid insurancePolicyId)
    {
        InsurancePolicy? policy = await _context.InsurancePolicies
            .AsNoTracking()
            .Include(i => i.Patient)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(i =>
                i.InsurancePolicyId == insurancePolicyId);

        if (policy == null)
        {
            return null;
        }

        User? user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == userId);

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

        bool hasAccess =
            isAdministrator ||
            isReceptionist ||
            (isPatient &&
             policy.Patient?.UserId == userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view this insurance policy.");
        }

        if (policy.Patient == null ||
            policy.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        return new InsurancePolicyResponse
        {
            InsurancePolicyId = policy.InsurancePolicyId,
            PatientId = policy.PatientId,
            PatientName = policy.Patient.User.FullName,

            ProviderName = policy.ProviderName,
            PolicyNumber = policy.PolicyNumber,

            StartDate = policy.StartDate,
            EndDate = policy.EndDate,

            CoverageAmount = policy.CoverageAmount,
            Status = policy.Status
        };
    }
}