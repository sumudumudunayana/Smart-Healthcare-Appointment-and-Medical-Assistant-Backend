namespace SmartHealthcare.API.Models;

public class Patient
{
    public Guid PatientId { get; set; }

    public Guid UserId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? BloodGroup { get; set; }

    public string? Address { get; set; }

    public string? EmergencyContact { get; set; }

    // Navigation property

    public User? User { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public ICollection<PatientAllergy> Allergies { get; set; }
    = new List<PatientAllergy>();

    public ICollection<ChronicCondition> ChronicConditions { get; set; }
        = new List<ChronicCondition>();

    public ICollection<InsurancePolicy> InsurancePolicies { get; set; }
        = new List<InsurancePolicy>();

    public ICollection<AIWorkflow> AIWorkflows { get; set; }
        = new List<AIWorkflow>();
}