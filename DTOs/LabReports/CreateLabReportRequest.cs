namespace SmartHealthcare.API.DTOs.LabReports;

public class CreateLabReportRequest
{
    public Guid RecordId { get; set; }

    public string ReportName { get; set; } = string.Empty;

    public string? ReportType { get; set; }

    public string? FilePath { get; set; }
}