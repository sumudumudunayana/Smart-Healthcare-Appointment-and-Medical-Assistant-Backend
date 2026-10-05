using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public interface IAppointmentSchedulingTool
{
    Task<List<AppointmentAvailabilityResult>> FindAvailableSlotsAsync(
        AppointmentSchedulingRequest request);
}