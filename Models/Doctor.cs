namespace SmartHealthcare.API.Models;

public class Doctor
{
    public Guid DoctorId { get; set; }

    public Guid UserId { get; set; }

    public Guid SpecializationId { get; set; }

    public Guid? DepartmentId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public int Experience { get; set; }

    // Navigation properties

    public User? User { get; set; }

    public Specialization? Specialization { get; set; }

    public Department? Department { get; set; }

    public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();

    public ICollection<DoctorLeave> DoctorLeaves { get; set; } = new List<DoctorLeave>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}