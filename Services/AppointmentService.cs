using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Appointments;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class AppointmentService
{
    private readonly ApplicationDbContext _context;

    public AppointmentService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppointmentResponse>
        CreateAsync(
            Guid userId,
            CreateAppointmentRequest request)
    {
        // --------------------------------------------------------
        // Find the patient belonging to the logged-in user
        // --------------------------------------------------------
        Patient? patient =
            await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "The logged-in user is not registered as a patient."
            );
        }

        // --------------------------------------------------------
        // Validate doctor
        // --------------------------------------------------------
        Doctor? doctor =
            await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d =>
                    d.DoctorId == request.DoctorId);

        if (doctor == null)
        {
            throw new ArgumentException(
                "The selected doctor was not found."
            );
        }

        if (doctor.User == null ||
            doctor.User.Status != "Active")
        {
            throw new InvalidOperationException(
                "The selected doctor is not active."
            );
        }

        // --------------------------------------------------------
        // Validate schedule
        // --------------------------------------------------------
        DoctorSchedule? schedule =
            await _context.DoctorSchedules
                .FirstOrDefaultAsync(s =>
                    s.ScheduleId == request.ScheduleId);

        if (schedule == null)
        {
            throw new ArgumentException(
                "The selected schedule was not found."
            );
        }

        // --------------------------------------------------------
        // Make sure schedule belongs to selected doctor
        // --------------------------------------------------------
        if (schedule.DoctorId != request.DoctorId)
        {
            throw new ArgumentException(
                "The selected schedule does not belong to the selected doctor."
            );
        }

        // --------------------------------------------------------
        // Schedule must be available
        // --------------------------------------------------------
        if (!schedule.AvailabilityStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The selected schedule is currently unavailable."
            );
        }

        // --------------------------------------------------------
        // Validate appointment date is not in the past
        // --------------------------------------------------------
        DateOnly today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.AppointmentDate < today)
        {
            throw new ArgumentException(
                "Appointment date cannot be in the past."
            );
        }

        // --------------------------------------------------------
        // Validate appointment day matches schedule
        // --------------------------------------------------------
        string appointmentDay =
            request.AppointmentDate.DayOfWeek.ToString();

        if (!appointmentDay.Equals(
                schedule.DayOfWeek,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The selected appointment date does not match the doctor's schedule."
            );
        }

        // --------------------------------------------------------
        // Validate appointment time falls within schedule
        // --------------------------------------------------------
        if (request.AppointmentTime < schedule.StartTime ||
            request.AppointmentTime >= schedule.EndTime)
        {
            throw new ArgumentException(
                "The selected appointment time is outside the doctor's schedule."
            );
        }

        // --------------------------------------------------------
        // Prevent double booking
        // --------------------------------------------------------
        bool alreadyBooked =
            await _context.Appointments
                .AnyAsync(a =>
                    a.DoctorId == request.DoctorId &&
                    a.AppointmentDate ==
                        request.AppointmentDate &&
                    a.AppointmentTime ==
                        request.AppointmentTime);

        if (alreadyBooked)
        {
            throw new InvalidOperationException(
                "The selected appointment time is already booked."
            );
        }

        // --------------------------------------------------------
        // Create appointment
        // --------------------------------------------------------
        var appointment = new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = patient.PatientId,
            DoctorId = request.DoctorId,
            ScheduleId = request.ScheduleId,
            AppointmentDate =
                request.AppointmentDate,
            AppointmentTime =
                request.AppointmentTime,
            Status = "Scheduled",
            Symptoms =
                string.IsNullOrWhiteSpace(request.Symptoms)
                    ? null
                    : request.Symptoms.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);

        await _context.SaveChangesAsync();

        return await BuildAppointmentResponseAsync(
            appointment.AppointmentId);
    }


    public async Task<List<AppointmentResponse>>
    GetMyAppointmentsAsync(Guid userId)
    {
        Patient? patient =
            await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "The logged-in user is not registered as a patient."
            );
        }

        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p!.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .Where(a =>
                a.PatientId == patient.PatientId)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Select(a => new AppointmentResponse
            {
                AppointmentId =
                    a.AppointmentId,

                PatientId =
                    a.PatientId,

                PatientName =
                    a.Patient!.User!.FullName,

                DoctorId =
                    a.DoctorId,

                DoctorName =
                    a.Doctor!.User!.FullName,

                ScheduleId =
                    a.ScheduleId,

                AppointmentDate =
                    a.AppointmentDate,

                AppointmentTime =
                    a.AppointmentTime,

                Status =
                    a.Status,

                Symptoms =
                    a.Symptoms,

                CreatedAt =
                    a.CreatedAt
            })
            .ToListAsync();
    }





    public async Task<AppointmentResponse?> GetByIdAsync(
    Guid userId,
    Guid appointmentId)
    {
        Patient? patient =
            await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "The logged-in user is not registered as a patient."
            );
        }

        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p!.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .Where(a =>
                a.AppointmentId == appointmentId &&
                a.PatientId == patient.PatientId)
            .Select(a => new AppointmentResponse
            {
                AppointmentId =
                    a.AppointmentId,

                PatientId =
                    a.PatientId,

                PatientName =
                    a.Patient!.User!.FullName,

                DoctorId =
                    a.DoctorId,

                DoctorName =
                    a.Doctor!.User!.FullName,

                ScheduleId =
                    a.ScheduleId,

                AppointmentDate =
                    a.AppointmentDate,

                AppointmentTime =
                    a.AppointmentTime,

                Status =
                    a.Status,

                Symptoms =
                    a.Symptoms,

                CreatedAt =
                    a.CreatedAt
            })
            .FirstOrDefaultAsync();
    }




    public async Task<AppointmentResponse?>
    CancelAsync(
        Guid userId,
        Guid appointmentId)
    {
        Patient? patient =
            await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "The logged-in user is not registered as a patient."
            );
        }

        Appointment? appointment =
            await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.AppointmentId == appointmentId &&
                    a.PatientId == patient.PatientId);

        if (appointment == null)
        {
            return null;
        }

        if (appointment.Status.Equals(
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "This appointment has already been cancelled."
            );
        }

        if (appointment.Status.Equals(
                "Completed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A completed appointment cannot be cancelled."
            );
        }

        if (appointment.Status.Equals(
                "NoShow",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A no-show appointment cannot be cancelled."
            );
        }

        appointment.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return await BuildAppointmentResponseAsync(
            appointment.AppointmentId);
    }




    public async Task<List<AppointmentResponse>>
    GetDoctorAppointmentsAsync(Guid userId)
    {
        Doctor? doctor =
            await _context.Doctors
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId);

        if (doctor == null)
        {
            throw new InvalidOperationException(
                "The logged-in user is not registered as a doctor."
            );
        }

        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p!.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .Where(a =>
                a.DoctorId == doctor.DoctorId)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Select(a => new AppointmentResponse
            {
                AppointmentId =
                    a.AppointmentId,

                PatientId =
                    a.PatientId,

                PatientName =
                    a.Patient!.User!.FullName,

                DoctorId =
                    a.DoctorId,

                DoctorName =
                    a.Doctor!.User!.FullName,

                ScheduleId =
                    a.ScheduleId,

                AppointmentDate =
                    a.AppointmentDate,

                AppointmentTime =
                    a.AppointmentTime,

                Status =
                    a.Status,

                Symptoms =
                    a.Symptoms,

                CreatedAt =
                    a.CreatedAt
            })
            .ToListAsync();
    }






    private async Task<AppointmentResponse>
        BuildAppointmentResponseAsync(
            Guid appointmentId)
    {
        AppointmentResponse? appointment =
            await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p!.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(a =>
                    a.AppointmentId == appointmentId)
                .Select(a => new AppointmentResponse
                {
                    AppointmentId =
                        a.AppointmentId,

                    PatientId =
                        a.PatientId,

                    PatientName =
                        a.Patient!.User!.FullName,

                    DoctorId =
                        a.DoctorId,

                    DoctorName =
                        a.Doctor!.User!.FullName,

                    ScheduleId =
                        a.ScheduleId,

                    AppointmentDate =
                        a.AppointmentDate,

                    AppointmentTime =
                        a.AppointmentTime,

                    Status =
                        a.Status,

                    Symptoms =
                        a.Symptoms,

                    CreatedAt =
                        a.CreatedAt
                })
                .FirstOrDefaultAsync();

        if (appointment == null)
        {
            throw new InvalidOperationException(
                "The appointment could not be retrieved after creation."
            );
        }

        return appointment;
    }
}