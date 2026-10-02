namespace SmartHealthcare.API.DTOs.AI;

public class AgentRequest
{
    public string Request { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? BillId { get; set; }
}