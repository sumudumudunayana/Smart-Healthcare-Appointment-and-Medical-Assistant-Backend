using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public interface IMedicalSummaryTool
{
    Task<MedicalSummaryData?> GetPatientMedicalDataAsync(
        Guid patientId);
}