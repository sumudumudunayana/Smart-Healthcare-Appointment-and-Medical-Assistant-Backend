using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Tools;

public class MedicalSummaryTool : IMedicalSummaryTool
{
    private readonly ApplicationDbContext _context;

    public MedicalSummaryTool(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalSummaryData?> GetPatientMedicalDataAsync(
        Guid patientId)
    {
        var patient =
            await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Allergies)
                .Include(p => p.ChronicConditions)
                .FirstOrDefaultAsync(
                    p => p.PatientId == patientId);

        if (patient == null)
        {
            return null;
        }

        var medicalRecords =
            await _context.MedicalRecords
                .AsNoTracking()
                .Include(record => record.Prescriptions)
                .Include(record => record.LabReports)
                .Include(record => record.Doctor)
                    .ThenInclude(doctor => doctor!.User)
                .Include(record => record.Appointment)
                .Where(record =>
                    record.PatientId == patientId)
                .OrderByDescending(
                    record => record.CreatedAt)
                .ToListAsync();

        var appointments =
            await _context.Appointments
                .AsNoTracking()
                .Include(appointment => appointment.Doctor)
                    .ThenInclude(doctor => doctor!.User)
                .Where(appointment =>
                    appointment.PatientId == patientId)
                .OrderByDescending(
                    appointment => appointment.AppointmentDate)
                .ToListAsync();

        return new MedicalSummaryData
        {
            PatientId = patient.PatientId,

            PatientName =
                patient.User?.FullName
                ?? string.Empty,

            DateOfBirth =
                patient.DateOfBirth,

            Gender =
                patient.Gender,

            BloodGroup =
                patient.BloodGroup,

            Allergies =
                patient.Allergies
                    .Select(allergy =>
                        new MedicalSummaryAllergy
                        {
                            Name = allergy.AllergyName,
                            Reaction = allergy.Reaction,
                            Severity = allergy.Severity
                        })
                    .ToList(),

            ChronicConditions =
                patient.ChronicConditions
                    .Select(condition =>
                        new MedicalSummaryCondition
                        {
                            Name = condition.ConditionName,
                            Description = condition.Description,
                            DiagnosedDate =
                                condition.DiagnosedDate,
                            Status = condition.Status
                        })
                    .ToList(),

            MedicalRecords =
                medicalRecords
                    .Select(record =>
                        new MedicalSummaryRecord
                        {
                            RecordId =
                                record.RecordId,

                            CreatedAt =
                                record.CreatedAt,

                            Diagnosis =
                                record.Diagnosis,

                            Treatment =
                                record.Treatment,

                            Notes =
                                record.Notes,

                            DoctorName =
                                record.Doctor?.User?.FullName,

                            Prescriptions =
                                record.Prescriptions
                                    .Select(
                                        prescription =>
                                            new MedicalSummaryPrescription
                                            {
                                                Medicine =
                                                    prescription.Medicine,

                                                Dosage =
                                                    prescription.Dosage,

                                                Duration =
                                                    prescription.Duration,

                                                Frequency =
                                                    prescription.Frequency,

                                                Instructions =
                                                    prescription.Instructions
                                            })
                                    .ToList(),

                            LabReports =
                                record.LabReports
                                    .Select(
                                        report =>
                                            new MedicalSummaryLabReport
                                            {
                                                ReportName =
                                                    report.ReportName,

                                                ReportType =
                                                    report.ReportType,

                                                UploadedAt =
                                                    report.UploadedAt
                                            })
                                    .ToList()
                        })
                    .ToList(),

            AppointmentCount =
                appointments.Count,

            RecentAppointments =
                appointments
                    .Take(10)
                    .Select(
                        appointment =>
                            new MedicalSummaryAppointment
                            {
                                AppointmentDate =
                                    appointment.AppointmentDate,

                                AppointmentTime =
                                    appointment.AppointmentTime,

                                Status =
                                    appointment.Status,

                                Symptoms =
                                    appointment.Symptoms,

                                DoctorName =
                                    appointment.Doctor?
                                        .User?
                                        .FullName
                            })
                    .ToList()
        };
    }
}