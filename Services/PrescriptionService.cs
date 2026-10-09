using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Prescriptions;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class PrescriptionService
{
    private readonly ApplicationDbContext _context;

    public PrescriptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PrescriptionResponse> CreateAsync(
        Guid userId,
        CreatePrescriptionRequest request)
    {
        // Find the logged-in doctor
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

        // Validate required prescription fields
        if (string.IsNullOrWhiteSpace(request.Medicine))
        {
            throw new ArgumentException(
                "Medicine is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Dosage))
        {
            throw new ArgumentException(
                "Dosage is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Duration))
        {
            throw new ArgumentException(
                "Duration is required.");
        }

        // Find the medical record
        MedicalRecord? record = await _context.MedicalRecords
            .Include(r => r.Patient)
                .ThenInclude(p => p!.User)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(r =>
                r.RecordId == request.RecordId);

        if (record == null)
        {
            throw new ArgumentException(
                "Medical record was not found.");
        }

        // Make sure this medical record belongs
        // to the logged-in doctor
        if (record.DoctorId != doctor.DoctorId)
        {
            throw new InvalidOperationException(
                "You can only create prescriptions for your own medical records.");
        }

        if (record.Patient == null ||
            record.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        var prescription = new Prescription
        {
            PrescriptionId = Guid.NewGuid(),
            RecordId = record.RecordId,

            Medicine = request.Medicine.Trim(),
            Dosage = request.Dosage.Trim(),
            Duration = request.Duration.Trim(),

            Frequency = string.IsNullOrWhiteSpace(request.Frequency)
                ? null
                : request.Frequency.Trim(),

            Instructions = string.IsNullOrWhiteSpace(request.Instructions)
                ? null
                : request.Instructions.Trim(),

            CreatedAt = DateTime.UtcNow
        };

        _context.Prescriptions.Add(prescription);

        await _context.SaveChangesAsync();

        return new PrescriptionResponse
        {
            PrescriptionId = prescription.PrescriptionId,
            RecordId = record.RecordId,

            PatientId = record.PatientId,
            PatientName = record.Patient.User.FullName,

            DoctorId = record.DoctorId,
            DoctorName = doctor.User.FullName,

            Medicine = prescription.Medicine,
            Dosage = prescription.Dosage,
            Duration = prescription.Duration,
            Frequency = prescription.Frequency,
            Instructions = prescription.Instructions,

            CreatedAt = prescription.CreatedAt
        };
    }

    public async Task<List<PrescriptionResponse>> GetByRecordAsync(
        Guid userId,
        Guid recordId)
    {
        // Find the logged-in doctor
        Doctor? doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            throw new InvalidOperationException(
                "Doctor profile was not found.");
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Record)
                .ThenInclude(r => r!.Patient)
                    .ThenInclude(p => p!.User)
            .Include(p => p.Record)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(p =>
                p.RecordId == recordId &&
                p.Record!.DoctorId == doctor.DoctorId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionResponse
            {
                PrescriptionId = p.PrescriptionId,
                RecordId = p.RecordId,

                PatientId = p.Record!.PatientId,
                PatientName = p.Record.Patient!.User!.FullName,

                DoctorId = p.Record.DoctorId,
                DoctorName = p.Record.Doctor!.User!.FullName,

                Medicine = p.Medicine,
                Dosage = p.Dosage,
                Duration = p.Duration,
                Frequency = p.Frequency,
                Instructions = p.Instructions,

                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<PrescriptionResponse>> GetMyAsync(
        Guid userId)
    {
        // Find the logged-in patient
        Patient? patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            throw new InvalidOperationException(
                "Patient profile was not found.");
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Record)
                .ThenInclude(r => r!.Patient)
                    .ThenInclude(p => p!.User)
            .Include(p => p.Record)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(p =>
                p.Record!.PatientId == patient.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionResponse
            {
                PrescriptionId = p.PrescriptionId,
                RecordId = p.RecordId,

                PatientId = p.Record!.PatientId,
                PatientName = p.Record.Patient!.User!.FullName,

                DoctorId = p.Record.DoctorId,
                DoctorName = p.Record.Doctor!.User!.FullName,

                Medicine = p.Medicine,
                Dosage = p.Dosage,
                Duration = p.Duration,
                Frequency = p.Frequency,
                Instructions = p.Instructions,

                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }


    public async Task<List<PrescriptionResponse>> GetMyDoctorPrescriptionsAsync(
    Guid userId)
    {
        Doctor? doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            throw new InvalidOperationException("Doctor profile was not found.");

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Record)
                .ThenInclude(r => r!.Patient)
                    .ThenInclude(p => p!.User)
            .Include(p => p.Record)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(p =>
                p.Record!.DoctorId == doctor.DoctorId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionResponse
            {
                PrescriptionId = p.PrescriptionId,
                RecordId = p.RecordId,

                PatientId = p.Record!.PatientId,
                PatientName = p.Record.Patient!.User!.FullName,

                DoctorId = p.Record.DoctorId,
                DoctorName = p.Record.Doctor!.User!.FullName,

                Medicine = p.Medicine,
                Dosage = p.Dosage,
                Duration = p.Duration,
                Frequency = p.Frequency,
                Instructions = p.Instructions,

                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }
}