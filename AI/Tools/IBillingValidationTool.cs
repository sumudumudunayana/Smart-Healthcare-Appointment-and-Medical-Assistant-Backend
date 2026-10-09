using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public interface IBillingValidationTool
{
    Task<BillingValidationData?> GetBillingDataAsync(
        Guid billId);
}