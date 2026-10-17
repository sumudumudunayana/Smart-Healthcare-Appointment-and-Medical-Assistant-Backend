namespace SmartHealthcare.API.Models;

public class Specialization
{
    public Guid SpecializationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}