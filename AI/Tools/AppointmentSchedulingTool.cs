using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public class AppointmentSchedulingTool : IAppointmentSchedulingTool
{
    private readonly ApplicationDbContext _context;

    public AppointmentSchedulingTool(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentAvailabilityResult>> FindAvailableSlotsAsync(
        AppointmentSchedulingRequest request)
    {
        DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (!string.IsNullOrWhiteSpace(request.PreferredDate) &&
            DateOnly.TryParse(
                request.PreferredDate,
                out DateOnly parsedDate))
        {
            startDate = parsedDate;
        }

        DateOnly endDate = startDate.AddDays(14);

        var query =
            _context.DoctorSchedules
                .AsNoTracking()
                .Include(schedule => schedule.Doctor)
                    .ThenInclude(doctor => doctor!.User)
                .Include(schedule => schedule.Doctor)
                    .ThenInclude(doctor => doctor!.Specialization)
                .Include(schedule => schedule.Doctor)
                    .ThenInclude(doctor => doctor!.Department)
                .Where(schedule =>
                    schedule.AvailabilityStatus == "Available");

        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            string specialization =
                request.Specialization.Trim().ToLower();

            query = query.Where(schedule =>
                schedule.Doctor != null &&
                schedule.Doctor.Specialization != null &&
                schedule.Doctor.Specialization.Name.ToLower()
                    .Contains(specialization));
        }

        var schedules = await query.ToListAsync();

        List<AppointmentAvailabilityResult> availableSlots = [];

        for (
            DateOnly date = startDate;
            date <= endDate;
            date = date.AddDays(1))
        {
            string dayOfWeek = date.DayOfWeek.ToString();

            foreach (var schedule in schedules)
            {
                if (!string.Equals(
                        schedule.DayOfWeek,
                        dayOfWeek,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (schedule.Doctor == null)
                {
                    continue;
                }

                bool doctorOnLeave =
                    await _context.DoctorLeaves
                        .AsNoTracking()
                        .AnyAsync(leave =>
                            leave.DoctorId == schedule.DoctorId &&
                            leave.Status == "Approved" &&
                            leave.StartDate <= date &&
                            leave.EndDate >= date);

                if (doctorOnLeave)
                {
                    continue;
                }

                var bookedTimes =
                    await _context.Appointments
                        .AsNoTracking()
                        .Where(appointment =>
                            appointment.DoctorId == schedule.DoctorId &&
                            appointment.AppointmentDate == date)
                        .Select(appointment =>
                            appointment.AppointmentTime)
                        .ToListAsync();

                TimeOnly currentTime = schedule.StartTime;

                while (currentTime < schedule.EndTime)
                {
                    bool alreadyBooked =
                        bookedTimes.Contains(currentTime);

                    if (!alreadyBooked)
                    {
                        availableSlots.Add(
                            new AppointmentAvailabilityResult
                            {
                                DoctorId =
                                    schedule.DoctorId,

                                DoctorName =
                                    schedule.Doctor.User?.FullName
                                    ?? "Unknown Doctor",

                                ScheduleId =
                                    schedule.ScheduleId,

                                AppointmentDate =
                                    date,

                                AppointmentTime =
                                    currentTime,

                                Specialization =
                                    schedule.Doctor.Specialization?.Name
                                    ?? string.Empty,

                                Department =
                                    schedule.Doctor.Department?.DepartmentName
                            });
                    }

                    currentTime =
                        currentTime.AddMinutes(30);
                }
            }
        }

        return availableSlots
            .OrderBy(slot => slot.AppointmentDate)
            .ThenBy(slot => slot.AppointmentTime)
            .ToList();
    }
}