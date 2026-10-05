namespace SmartHealthcare.API.Models;

public class LabReport
{
    public Guid LabReportId { get; set; }

    public Guid PatientId { get; set; }

    public Guid RecordId { get; set; }

    public string ReportName { get; set; } = string.Empty;

    public string? ReportType { get; set; }

    public string? FilePath { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid? UploadedBy { get; set; }

    public Patient? Patient { get; set; }

    public MedicalRecord? Record { get; set; }
}