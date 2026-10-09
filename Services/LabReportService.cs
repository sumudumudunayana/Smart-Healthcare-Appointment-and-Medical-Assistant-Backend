using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.LabReports;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services;

public class LabReportService
{
    private readonly ApplicationDbContext _context;

    public LabReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LabReportResponse> CreateAsync(
        Guid userId,
        CreateLabReportRequest request)
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

        // Validate required report name
        if (string.IsNullOrWhiteSpace(request.ReportName))
        {
            throw new ArgumentException(
                "Report name is required.");
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

        // Make sure the record belongs to the logged-in doctor
        if (record.DoctorId != doctor.DoctorId)
        {
            throw new InvalidOperationException(
                "You can only add lab reports to your own medical records.");
        }

        if (record.Patient == null ||
            record.Patient.User == null)
        {
            throw new InvalidOperationException(
                "Patient information was not found.");
        }

        var labReport = new LabReport
        {
            LabReportId = Guid.NewGuid(),
            PatientId = record.PatientId,
            RecordId = record.RecordId,

            ReportName = request.ReportName.Trim(),

            ReportType = string.IsNullOrWhiteSpace(request.ReportType)
                ? null
                : request.ReportType.Trim(),

            FilePath = string.IsNullOrWhiteSpace(request.FilePath)
                ? null
                : request.FilePath.Trim(),

            UploadedAt = DateTime.UtcNow,
            UploadedBy = userId
        };

        _context.LabReports.Add(labReport);

        await _context.SaveChangesAsync();

        return new LabReportResponse
        {
            LabReportId = labReport.LabReportId,

            PatientId = record.PatientId,
            PatientName = record.Patient.User.FullName,

            RecordId = record.RecordId,

            DoctorId = record.DoctorId,
            DoctorName = doctor.User.FullName,

            ReportName = labReport.ReportName,
            ReportType = labReport.ReportType,
            FilePath = labReport.FilePath,

            UploadedAt = labReport.UploadedAt,
            UploadedBy = labReport.UploadedBy
        };
    }

    public async Task<List<LabReportResponse>> GetByRecordAsync(
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

        return await _context.LabReports
            .AsNoTracking()
            .Include(l => l.Patient)
                .ThenInclude(p => p!.User)
            .Include(l => l.Record)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(l =>
                l.RecordId == recordId &&
                l.Record!.DoctorId == doctor.DoctorId)
            .OrderByDescending(l => l.UploadedAt)
            .Select(l => new LabReportResponse
            {
                LabReportId = l.LabReportId,

                PatientId = l.PatientId,
                PatientName = l.Patient!.User!.FullName,

                RecordId = l.RecordId,

                DoctorId = l.Record!.DoctorId,
                DoctorName = l.Record.Doctor!.User!.FullName,

                ReportName = l.ReportName,
                ReportType = l.ReportType,
                FilePath = l.FilePath,

                UploadedAt = l.UploadedAt,
                UploadedBy = l.UploadedBy
            })
            .ToListAsync();
    }

    public async Task<List<LabReportResponse>> GetMyAsync(
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

        return await _context.LabReports
            .AsNoTracking()
            .Include(l => l.Patient)
                .ThenInclude(p => p!.User)
            .Include(l => l.Record)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.User)
            .Where(l =>
                l.PatientId == patient.PatientId)
            .OrderByDescending(l => l.UploadedAt)
            .Select(l => new LabReportResponse
            {
                LabReportId = l.LabReportId,

                PatientId = l.PatientId,
                PatientName = l.Patient!.User!.FullName,

                RecordId = l.RecordId,

                DoctorId = l.Record!.DoctorId,
                DoctorName = l.Record.Doctor!.User!.FullName,

                ReportName = l.ReportName,
                ReportType = l.ReportType,
                FilePath = l.FilePath,

                UploadedAt = l.UploadedAt,
                UploadedBy = l.UploadedBy
            })
            .ToListAsync();
    }


    public async Task<List<LabReportResponse>> GetMyDoctorReportsAsync(
    Guid userId)
{
    Doctor? doctor = await _context.Doctors
        .FirstOrDefaultAsync(d => d.UserId == userId);

    if (doctor == null)
    {
        throw new InvalidOperationException(
            "Doctor profile was not found.");
    }

    return await _context.LabReports
        .AsNoTracking()
        .Include(l => l.Patient)
            .ThenInclude(p => p!.User)
        .Include(l => l.Record)
            .ThenInclude(r => r!.Doctor)
                .ThenInclude(d => d!.User)
        .Where(l =>
            l.Record!.DoctorId == doctor.DoctorId)
        .OrderByDescending(l => l.UploadedAt)
        .Select(l => new LabReportResponse
        {
            LabReportId = l.LabReportId,

            PatientId = l.PatientId,
            PatientName = l.Patient!.User!.FullName,

            RecordId = l.RecordId,

            DoctorId = l.Record!.DoctorId,
            DoctorName = l.Record.Doctor!.User!.FullName,

            ReportName = l.ReportName,
            ReportType = l.ReportType,
            FilePath = l.FilePath,

            UploadedAt = l.UploadedAt,
            UploadedBy = l.UploadedBy
        })
        .ToListAsync();
}
}