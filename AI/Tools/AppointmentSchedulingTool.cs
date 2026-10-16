using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public class AppointmentSchedulingTool : IAppointmentSchedulingTool
{
    private readonly ApplicationDbContext _context;

    // The clinic operates in Sri Lanka Standard Time (UTC+05:30).
    private static readonly TimeSpan SriLankaOffset =
        TimeSpan.FromHours(5.5);

    public AppointmentSchedulingTool(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentAvailabilityResult>>
        FindAvailableSlotsAsync(
            AppointmentSchedulingRequest request)
    {
        DateTime localNow =
            DateTimeOffset.UtcNow
                .ToOffset(SriLankaOffset)
                .DateTime;

        DateOnly today =
            DateOnly.FromDateTime(localNow);

        TimeOnly currentTime =
            TimeOnly.FromDateTime(localNow);

        DateOnly startDate = today;

        if (!string.IsNullOrWhiteSpace(request.PreferredDate))
        {
            string preferredDate = request.PreferredDate
                .Trim()
                .ToLowerInvariant();

            DateOnly parsedDate;

            switch (preferredDate)
            {
                case "today":
                    parsedDate = today;
                    break;

                case "tomorrow":
                    parsedDate = today.AddDays(1);
                    break;

                case "day after tomorrow":
                    parsedDate = today.AddDays(2);
                    break;

                default:
                    if (!DateOnly.TryParse(
                            request.PreferredDate,
                            out parsedDate))
                    {
                        return [];
                    }

                    break;
            }

            // Do not return appointments in the past.
            if (parsedDate < today)
            {
                return [];
            }

            startDate = parsedDate;
        }

        DateOnly endDate = startDate.AddDays(14);

        // Time preference boundaries.
        TimeOnly? preferredStartTime = null;
        TimeOnly? preferredEndTime = null;

        if (!string.IsNullOrWhiteSpace(request.PreferredTimePeriod))
        {
            string period =
                request.PreferredTimePeriod.Trim().ToLowerInvariant();

            switch (period)
            {
                case "morning":
                    preferredStartTime = new TimeOnly(6, 0);
                    preferredEndTime = new TimeOnly(12, 0);
                    break;

                case "afternoon":
                    preferredStartTime = new TimeOnly(12, 0);
                    preferredEndTime = new TimeOnly(17, 0);
                    break;

                case "evening":
                    preferredStartTime = new TimeOnly(17, 0);
                    preferredEndTime = new TimeOnly(21, 0);
                    break;

                case "night":
                    preferredStartTime = new TimeOnly(18, 0);
                    preferredEndTime = new TimeOnly(23, 0);
                    break;
            }
        }

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
                    schedule.AvailabilityStatus == "Available" &&
                    schedule.StartTime < schedule.EndTime);

        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            string specialization =
                request.Specialization.Trim().ToLowerInvariant();

            // Normalize common variations of medical specialization names.
            specialization = specialization switch
            {
                "cardiologist" => "cardiology",
                "dermatologist" => "dermatology",
                "neurologist" => "neurology",
                "psychiatrist" => "psychiatry",
                _ => specialization
            };

            query = query.Where(schedule =>
                schedule.Doctor != null &&
                schedule.Doctor.Specialization != null &&
                schedule.Doctor.Specialization.Name
                    .ToLower()
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

                // Treat all appointments except cancelled ones as
                // blocking slots. Update these values if your
                // application uses different cancellation statuses.
                var bookedTimes =
                    await _context.Appointments
                        .AsNoTracking()
                        .Where(appointment =>
                            appointment.DoctorId == schedule.DoctorId &&
                            appointment.AppointmentDate == date &&
                            appointment.Status.ToLower() != "cancelled" &&
                            appointment.Status.ToLower() != "canceled")
                        .Select(appointment => appointment.AppointmentTime)
                        .ToListAsync();

                TimeOnly currentSlot = schedule.StartTime;

                while (currentSlot < schedule.EndTime)
                {
                    TimeOnly slotEnd;

                    try
                    {
                        slotEnd = currentSlot.AddMinutes(30);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }

                    // Do not offer a slot that extends beyond
                    // the schedule's end time.
                    if (slotEnd > schedule.EndTime)
                    {
                        break;
                    }

                    // Exclude time slots that have already passed.
                    bool isInPast =
                        date == today &&
                        currentSlot <= currentTime;

                    bool alreadyBooked =
                        bookedTimes.Contains(currentSlot);

                    bool matchesTimePreference =
                        (!preferredStartTime.HasValue ||
                         currentSlot >= preferredStartTime.Value) &&
                        (!preferredEndTime.HasValue ||
                         currentSlot < preferredEndTime.Value);

                    if (!isInPast &&
                        !alreadyBooked &&
                        matchesTimePreference)
                    {
                        availableSlots.Add(
                            new AppointmentAvailabilityResult
                            {
                                DoctorId = schedule.DoctorId,

                                DoctorName =
                                    schedule.Doctor.User?.FullName
                                    ?? "Unknown Doctor",

                                ScheduleId = schedule.ScheduleId,

                                AppointmentDate = date,

                                AppointmentTime = currentSlot,

                                Specialization =
                                    schedule.Doctor.Specialization?.Name
                                    ?? string.Empty,

                                Department =
                                    schedule.Doctor.Department?.DepartmentName
                            });
                    }

                    currentSlot = slotEnd;
                }
            }
        }

        return availableSlots
            .OrderBy(slot => slot.AppointmentDate)
            .ThenBy(slot => slot.AppointmentTime)
            .ToList();
    }
}