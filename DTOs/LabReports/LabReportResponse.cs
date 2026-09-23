namespace SmartHealthcare.API.DTOs.LabReports;

public class LabReportResponse
{
    public Guid LabReportId { get; set; }

    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid RecordId { get; set; }

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public string ReportName { get; set; } = string.Empty;

    public string? ReportType { get; set; }

    public string? FilePath { get; set; }

    public DateTime UploadedAt { get; set; }

    public Guid? UploadedBy { get; set; }
}