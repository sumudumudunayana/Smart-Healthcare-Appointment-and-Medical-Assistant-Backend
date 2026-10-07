namespace SmartHealthcare.API.DTOs.AI;

public class MedicalSummaryData
{
    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? BloodGroup { get; set; }

    public List<MedicalSummaryAllergy> Allergies { get; set; }
        = new();

    public List<MedicalSummaryCondition> ChronicConditions { get; set; }
        = new();

    public List<MedicalSummaryRecord> MedicalRecords { get; set; }
        = new();

    public int AppointmentCount { get; set; }

    public List<MedicalSummaryAppointment> RecentAppointments { get; set; }
        = new();
}

public class MedicalSummaryAllergy
{
    public string Name { get; set; } = string.Empty;

    public string? Reaction { get; set; }

    public string? Severity { get; set; }
}

public class MedicalSummaryCondition
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DiagnosedDate { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class MedicalSummaryRecord
{
    public Guid RecordId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Diagnosis { get; set; }

    public string? Treatment { get; set; }

    public string? Notes { get; set; }

    public string? DoctorName { get; set; }

    public List<MedicalSummaryPrescription> Prescriptions { get; set; }
        = new();

    public List<MedicalSummaryLabReport> LabReports { get; set; }
        = new();
}

public class MedicalSummaryPrescription
{
    public string Medicine { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string? Frequency { get; set; }

    public string? Instructions { get; set; }
}

public class MedicalSummaryLabReport
{
    public string ReportName { get; set; } = string.Empty;

    public string? ReportType { get; set; }

    public DateTime UploadedAt { get; set; }
}

public class MedicalSummaryAppointment
{
    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Symptoms { get; set; }

    public string? DoctorName { get; set; }
}