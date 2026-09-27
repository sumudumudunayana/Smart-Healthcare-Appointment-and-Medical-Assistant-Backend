using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.MedicalRecords;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class MedicalRecordService
{
    private readonly ApplicationDbContext _context;

    public MedicalRecordService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalRecordResponse> CreateAsync(
        Guid userId,
        CreateMedicalRecordRequest request)
    {
        // Find the doctor using the logged-in user's ID
        Doctor? doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            throw new InvalidOperationException(
                "Doctor profile was not found.");
        }

        if (doctor.User == null)
        {
            throw new InvalidOperationException(
                "Doctor user profile was not found.");
        }

        // Find the appointment
        Appointment? appointment = await _context.Appointments
            .Include(a => a.Patient)
                .ThenInclude(p => p!.User)
                    .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == request.AppointmentId);

        if (appointment == null)
        {
            throw new ArgumentException(
                "Appointment was not found.");
        }

        if (appointment.Patient == null ||
            appointment.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        // Make sure the appointment belongs to this doctor
        if (appointment.DoctorId != doctor.DoctorId)
        {
            throw new InvalidOperationException(
                "You can only create a medical record for your own appointments.");
        }

        // Don't allow more than one medical record
        // for the same appointment
        bool recordExists = await _context.MedicalRecords
            .AnyAsync(r =>
                r.AppointmentId == request.AppointmentId);

        if (recordExists)
        {
            throw new InvalidOperationException(
                "A medical record already exists for this appointment.");
        }

        // A cancelled appointment should not receive
        // a medical record
        if (appointment.Status == "Cancelled")
        {
            throw new InvalidOperationException(
                "A medical record cannot be created for a cancelled appointment.");
        }

        var record = new MedicalRecord
        {
            RecordId = Guid.NewGuid(),
            PatientId = appointment.PatientId,
            DoctorId = doctor.DoctorId,
            AppointmentId = appointment.AppointmentId,

            Diagnosis = string.IsNullOrWhiteSpace(request.Diagnosis)
                ? null
                : request.Diagnosis.Trim(),

            Treatment = string.IsNullOrWhiteSpace(request.Treatment)
                ? null
                : request.Treatment.Trim(),

            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim(),

            CreatedAt = DateTime.UtcNow
        };

        _context.MedicalRecords.Add(record);

        await _context.SaveChangesAsync();

        return new MedicalRecordResponse
        {
            RecordId = record.RecordId,

            PatientId = appointment.PatientId,
            PatientName = appointment.Patient.User.FullName,

            DoctorId = doctor.DoctorId,
            DoctorName = doctor.User.FullName,

            AppointmentId = appointment.AppointmentId,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentTime = appointment.AppointmentTime,

            Diagnosis = record.Diagnosis,
            Treatment = record.Treatment,
            Notes = record.Notes,

            CreatedAt = record.CreatedAt
        };
    }

    public async Task<MedicalRecordResponse?> GetByAppointmentAsync(
        Guid userId,
        Guid appointmentId)
    {
        // Find the doctor using the logged-in user's ID
        Doctor? doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            throw new InvalidOperationException(
                "Doctor profile was not found.");
        }

        if (doctor.User == null)
        {
            throw new InvalidOperationException(
                "Doctor user profile was not found.");
        }

        MedicalRecord? record = await _context.MedicalRecords
           .Include(r => r.Patient)
            .ThenInclude(p => p!.User)
                .Include(r => r.Doctor)
            .ThenInclude(d => d!.User)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r =>
                r.AppointmentId == appointmentId &&
                r.DoctorId == doctor.DoctorId);

        if (record == null)
        {
            return null;
        }

        if (record.Patient == null ||
            record.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        if (record.Doctor == null ||
            record.Doctor.User == null)
        {
            throw new InvalidOperationException(
                "Doctor information was not found.");
        }

        if (record.Appointment == null)
        {
            throw new InvalidOperationException(
                "Appointment information was not found.");
        }

        return new MedicalRecordResponse
        {
            RecordId = record.RecordId,

            PatientId = record.PatientId,
            PatientName = record.Patient.User.FullName,

            DoctorId = record.DoctorId,
            DoctorName = record.Doctor.User.FullName,

            AppointmentId = record.AppointmentId,
            AppointmentDate = record.Appointment.AppointmentDate,
            AppointmentTime = record.Appointment.AppointmentTime,

            Diagnosis = record.Diagnosis,
            Treatment = record.Treatment,
            Notes = record.Notes,

            CreatedAt = record.CreatedAt
        };
    }


    public async Task<List<MedicalRecordResponse>> GetMyRecordsAsync(
    Guid userId)
    {
        Patient? patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "Patient profile was not found.");
        }

        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(r => r.Patient)
                .ThenInclude(p => p!.User)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.Appointment)
            .Where(r => r.PatientId == patient.PatientId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MedicalRecordResponse
            {
                RecordId = r.RecordId,

                PatientId = r.PatientId,
                PatientName = r.Patient!.User!.FullName,

                DoctorId = r.DoctorId,
                DoctorName = r.Doctor!.User!.FullName,

                AppointmentId = r.AppointmentId,
                AppointmentDate = r.Appointment!.AppointmentDate,
                AppointmentTime = r.Appointment.AppointmentTime,

                Diagnosis = r.Diagnosis,
                Treatment = r.Treatment,
                Notes = r.Notes,

                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }
}