using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public class BillingValidationTool : IBillingValidationTool
{
    private readonly ApplicationDbContext _context;

    public BillingValidationTool(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BillingValidationData?> GetBillingDataAsync(
        Guid billId)
    {
        var bill =
            await _context.Bills
                .AsNoTracking()
                .Include(bill => bill.Patient)
                    .ThenInclude(patient => patient!.User)
                .Include(bill => bill.Appointment)
                .Include(bill => bill.Payments)
                .Include(bill => bill.InsuranceClaims)
                    .ThenInclude(claim => claim.InsurancePolicy)
                .FirstOrDefaultAsync(
                    bill => bill.BillId == billId);

        if (bill == null)
        {
            return null;
        }

        var insurancePolicies =
            await _context.InsurancePolicies
                .AsNoTracking()
                .Where(policy =>
                    policy.PatientId == bill.PatientId)
                .ToListAsync();

        return new BillingValidationData
        {
            BillId = bill.BillId,

            AppointmentId = bill.AppointmentId,

            PatientId = bill.PatientId,

            TotalAmount = bill.TotalAmount,

            BillStatus = bill.BillStatus,

            GeneratedDate = bill.GeneratedDate,

            PatientName =
                bill.Patient?.User?.FullName,

            AppointmentDate =
                bill.Appointment?.AppointmentDate,

            AppointmentTime =
                bill.Appointment?.AppointmentTime,

            AppointmentStatus =
                bill.Appointment?.Status,

            Payments =
                bill.Payments
                    .Select(payment =>
                        new BillingPaymentData
                        {
                            PaymentId =
                                payment.PaymentId,

                            Amount =
                                payment.Amount,

                            PaymentMethod =
                                payment.PaymentMethod,

                            PaymentDate =
                                payment.PaymentDate,

                            PaymentStatus =
                                payment.PaymentStatus
                        })
                    .ToList(),

            InsuranceClaims =
                bill.InsuranceClaims
                    .Select(claim =>
                        new BillingInsuranceClaimData
                        {
                            ClaimId =
                                claim.ClaimId,

                            InsurancePolicyId =
                                claim.InsurancePolicyId,

                            ClaimAmount =
                                claim.ClaimAmount,

                            Status =
                                claim.Status,

                            ClaimDetails =
                                claim.ClaimDetails,

                            SubmittedAt =
                                claim.SubmittedAt,

                            ProcessedAt =
                                claim.ProcessedAt
                        })
                    .ToList(),

            InsurancePolicies =
                insurancePolicies
                    .Select(policy =>
                        new BillingInsurancePolicyData
                        {
                            InsurancePolicyId =
                                policy.InsurancePolicyId,

                            ProviderName =
                                policy.ProviderName,

                            PolicyNumber =
                                policy.PolicyNumber,

                            StartDate =
                                policy.StartDate,

                            EndDate =
                                policy.EndDate,

                            CoverageAmount =
                                policy.CoverageAmount,

                            Status =
                                policy.Status
                        })
                    .ToList()
        };
    }
}