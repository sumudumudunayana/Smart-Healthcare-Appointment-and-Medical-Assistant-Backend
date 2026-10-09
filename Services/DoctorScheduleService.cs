using Microsoft.EntityFrameworkCore;

using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.DoctorSchedules;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class DoctorScheduleService
{
    private readonly ApplicationDbContext _context;

    public DoctorScheduleService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET DOCTOR ID FROM AUTHENTICATED USER ID
    // ============================================================
    public async Task<Guid?> GetDoctorIdFromUserAsync(
        Guid userId)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.DoctorId)
            .FirstOrDefaultAsync();
    }

    // ============================================================
    // CREATE DOCTOR SCHEDULE
    // ============================================================
    public async Task<DoctorScheduleResponse> CreateAsync(
        CreateDoctorScheduleRequest request)
    {
        if (request.DoctorId == Guid.Empty)
        {
            throw new ArgumentException(
                "Doctor ID is required."
            );
        }

        if (string.IsNullOrWhiteSpace(
                request.DayOfWeek))
        {
            throw new ArgumentException(
                "Day of week is required."
            );
        }

        string dayOfWeek =
            request.DayOfWeek.Trim();

        string[] validDays =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        string? matchedDay =
            validDays.FirstOrDefault(day =>
                day.Equals(
                    dayOfWeek,
                    StringComparison.OrdinalIgnoreCase));

        if (matchedDay == null)
        {
            throw new ArgumentException(
                "Invalid day of week."
            );
        }

        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException(
                "Start time must be before end time."
            );
        }

        string availabilityStatus =
            string.IsNullOrWhiteSpace(
                request.AvailabilityStatus)
                ? "Available"
                : request.AvailabilityStatus.Trim();

        if (!availabilityStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase)
            &&
            !availabilityStatus.Equals(
                "Unavailable",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Availability status must be Available or Unavailable."
            );
        }

        if (availabilityStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase))
        {
            availabilityStatus = "Available";
        }
        else
        {
            availabilityStatus = "Unavailable";
        }

        // --------------------------------------------------------
        // Verify doctor exists
        // --------------------------------------------------------
        bool doctorExists =
            await _context.Doctors
                .AnyAsync(d =>
                    d.DoctorId == request.DoctorId);

        if (!doctorExists)
        {
            throw new ArgumentException(
                "The selected doctor was not found."
            );
        }

        // --------------------------------------------------------
        // Prevent duplicate schedule
        // --------------------------------------------------------
        bool duplicateExists =
            await _context.DoctorSchedules
                .AnyAsync(s =>
                    s.DoctorId == request.DoctorId &&
                    s.DayOfWeek == matchedDay &&
                    s.StartTime == request.StartTime &&
                    s.EndTime == request.EndTime);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "A schedule with the same day and time already exists for this doctor."
            );
        }

        var schedule = new DoctorSchedule
        {
            ScheduleId = Guid.NewGuid(),

            DoctorId = request.DoctorId,

            DayOfWeek = matchedDay,

            StartTime = request.StartTime,

            EndTime = request.EndTime,

            AvailabilityStatus = availabilityStatus
        };

        _context.DoctorSchedules.Add(schedule);

        await _context.SaveChangesAsync();

        return await BuildScheduleResponseAsync(
            schedule.ScheduleId);
    }

    // ============================================================
    // GET SCHEDULES FOR A DOCTOR
    // ============================================================
    public async Task<List<DoctorScheduleResponse>>
        GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Include(s => s.Doctor)
                .ThenInclude(d => d!.User)
            .Where(s =>
                s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new DoctorScheduleResponse
            {
                ScheduleId = s.ScheduleId,

                DoctorId = s.DoctorId,

                DoctorName =
                    s.Doctor!.User!.FullName,

                DayOfWeek = s.DayOfWeek,

                StartTime = s.StartTime,

                EndTime = s.EndTime,

                AvailabilityStatus =
                    s.AvailabilityStatus
            })
            .ToListAsync();
    }

    // ============================================================
    // UPDATE DOCTOR SCHEDULE
    // ============================================================
    public async Task<DoctorScheduleResponse?>
        UpdateAsync(
            Guid scheduleId,
            UpdateDoctorScheduleRequest request,
            Guid? authenticatedDoctorId = null)
    {
        DoctorSchedule? schedule =
            await _context.DoctorSchedules
                .FirstOrDefaultAsync(s =>
                    s.ScheduleId == scheduleId);

        if (schedule == null)
        {
            return null;
        }

        // --------------------------------------------------------
        // Ownership check
        // --------------------------------------------------------
        // authenticatedDoctorId is null for Administrator.
        // If a doctor is performing the operation, the schedule
        // must belong to that doctor.
        // --------------------------------------------------------
        if (authenticatedDoctorId.HasValue &&
            schedule.DoctorId != authenticatedDoctorId.Value)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to update this schedule."
            );
        }

        if (string.IsNullOrWhiteSpace(
                request.DayOfWeek))
        {
            throw new ArgumentException(
                "Day of week is required."
            );
        }

        string dayOfWeek =
            request.DayOfWeek.Trim();

        string[] validDays =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        string? matchedDay =
            validDays.FirstOrDefault(day =>
                day.Equals(
                    dayOfWeek,
                    StringComparison.OrdinalIgnoreCase));

        if (matchedDay == null)
        {
            throw new ArgumentException(
                "Invalid day of week."
            );
        }

        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException(
                "Start time must be before end time."
            );
        }

        string availabilityStatus =
            string.IsNullOrWhiteSpace(
                request.AvailabilityStatus)
                ? "Available"
                : request.AvailabilityStatus.Trim();

        if (!availabilityStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase)
            &&
            !availabilityStatus.Equals(
                "Unavailable",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Availability status must be Available or Unavailable."
            );
        }

        if (availabilityStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase))
        {
            availabilityStatus = "Available";
        }
        else
        {
            availabilityStatus = "Unavailable";
        }

        // --------------------------------------------------------
        // Prevent duplicate schedule
        // --------------------------------------------------------
        bool duplicateExists =
            await _context.DoctorSchedules
                .AnyAsync(s =>
                    s.ScheduleId != scheduleId &&
                    s.DoctorId == schedule.DoctorId &&
                    s.DayOfWeek == matchedDay &&
                    s.StartTime == request.StartTime &&
                    s.EndTime == request.EndTime);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "A schedule with the same day and time already exists for this doctor."
            );
        }

        schedule.DayOfWeek = matchedDay;

        schedule.StartTime = request.StartTime;

        schedule.EndTime = request.EndTime;

        schedule.AvailabilityStatus =
            availabilityStatus;

        await _context.SaveChangesAsync();

        return await BuildScheduleResponseAsync(
            scheduleId);
    }

    // ============================================================
    // DELETE DOCTOR SCHEDULE
    // ============================================================
    public async Task<bool> DeleteAsync(
        Guid scheduleId,
        Guid? authenticatedDoctorId = null)
    {
        DoctorSchedule? schedule =
            await _context.DoctorSchedules
                .FirstOrDefaultAsync(s =>
                    s.ScheduleId == scheduleId);

        if (schedule == null)
        {
            return false;
        }

        // --------------------------------------------------------
        // Ownership check
        // --------------------------------------------------------
        // Administrator:
        // authenticatedDoctorId == null
        //
        // Doctor:
        // authenticatedDoctorId contains their DoctorId
        // --------------------------------------------------------
        if (authenticatedDoctorId.HasValue &&
            schedule.DoctorId != authenticatedDoctorId.Value)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to delete this schedule."
            );
        }

        _context.DoctorSchedules.Remove(
            schedule);

        await _context.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // BUILD SCHEDULE RESPONSE
    // ============================================================
    private async Task<DoctorScheduleResponse>
        BuildScheduleResponseAsync(
            Guid scheduleId)
    {
        DoctorScheduleResponse? schedule =
            await _context.DoctorSchedules
                .AsNoTracking()
                .Include(s => s.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(s =>
                    s.ScheduleId == scheduleId)
                .Select(s => new DoctorScheduleResponse
                {
                    ScheduleId = s.ScheduleId,

                    DoctorId = s.DoctorId,

                    DoctorName =
                        s.Doctor!.User!.FullName,

                    DayOfWeek = s.DayOfWeek,

                    StartTime = s.StartTime,

                    EndTime = s.EndTime,

                    AvailabilityStatus =
                        s.AvailabilityStatus
                })
                .FirstOrDefaultAsync();

        if (schedule == null)
        {
            throw new InvalidOperationException(
                "The schedule could not be retrieved after creation."
            );
        }

        return schedule;
    }
}