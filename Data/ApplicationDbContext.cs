using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // =========================
    // DbSets
    // =========================

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Specialization> Specializations => Set<Specialization>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<DoctorLeave> DoctorLeaves => Set<DoctorLeave>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<LabReport> LabReports => Set<LabReport>();

    public DbSet<Bill> Bills => Set<Bill>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();

    public DbSet<ChronicCondition> ChronicConditions => Set<ChronicCondition>();

    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();

    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();

    public DbSet<AIWorkflow> AIWorkflows => Set<AIWorkflow>();

    public DbSet<AIWorkflowStep> AIWorkflowSteps => Set<AIWorkflowStep>();

    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();

    public DbSet<AIApproval> AIApprovals => Set<AIApproval>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
    new Role
    {
        RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        RoleName = "Administrator"
    },
    new Role
    {
        RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        RoleName = "Doctor"
    },
    new Role
    {
        RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        RoleName = "Receptionist"
    },
    new Role
    {
        RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        RoleName = "Patient"
    }
);


        // ============================================================
        // ROLE
        // ============================================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(r => r.RoleName)
                .IsUnique();
        });


        // ============================================================
        // USER
        // ============================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Phone)
                .HasMaxLength(20);

            entity.Property(u => u.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(u => u.CreatedAt)
                .IsRequired();


            // User -> Role
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);


            // User -> Patient (1:1)
            entity.HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // User -> Doctor (1:1)
            entity.HasOne(u => u.Doctor)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // PATIENT
        // ============================================================

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.PatientId);

            entity.Property(p => p.Gender)
                .HasMaxLength(30);

            entity.Property(p => p.BloodGroup)
                .HasMaxLength(10);

            entity.Property(p => p.Address)
                .HasMaxLength(300);

            entity.Property(p => p.EmergencyContact)
                .HasMaxLength(20);

            // One user can have only one patient profile
            entity.HasIndex(p => p.UserId)
                .IsUnique();
        });


        // ============================================================
        // DEPARTMENT
        // ============================================================

        modelBuilder.Entity<Department>(entity =>
{
    entity.HasKey(d => d.DepartmentId);

    entity.Property(d => d.DepartmentName)
        .IsRequired()
        .HasMaxLength(100);

    entity.Property(d => d.Description)
        .HasMaxLength(500);

    entity.Property(d => d.Location)
        .HasMaxLength(200);

    entity.HasIndex(d => d.DepartmentName)
        .IsUnique();
});


        // ============================================================
        // SPECIALIZATION
        // ============================================================

        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.HasKey(s => s.SpecializationId);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(s => s.Name)
                .IsUnique();
        });


        // ============================================================
        // DOCTOR
        // ============================================================

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(d => d.DoctorId);

            entity.Property(d => d.LicenseNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Experience)
                .IsRequired();

            // One user = one doctor profile
            entity.HasIndex(d => d.UserId)
                .IsUnique();

            // License number must be unique
            entity.HasIndex(d => d.LicenseNumber)
                .IsUnique();


            // Doctor -> Specialization
            entity.HasOne(d => d.Specialization)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);


            // Doctor -> Department
            entity.HasOne(d => d.Department)
                .WithMany(dep => dep.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);


            // Experience cannot be negative
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Doctors_Experience",
                    "\"Experience\" >= 0"
                );
            });
        });


        // ============================================================
        // DOCTOR SCHEDULE
        // ============================================================

        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(s => s.ScheduleId);

            entity.Property(s => s.DayOfWeek)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(s => s.AvailabilityStatus)
                .IsRequired()
                .HasMaxLength(30);


            // Doctor -> Schedules (1:M)
            entity.HasOne(s => s.Doctor)
                .WithMany(d => d.DoctorSchedules)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);


            // Start time must be before end time
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_DoctorSchedules_TimeRange",
                    "\"StartTime\" < \"EndTime\""
                );
            });


            // Prevent duplicate schedule for same doctor/day/time
            entity.HasIndex(s => new
            {
                s.DoctorId,
                s.DayOfWeek,
                s.StartTime,
                s.EndTime
            })
            .IsUnique();
        });


        // ============================================================
        // DOCTOR LEAVE
        // ============================================================

        modelBuilder.Entity<DoctorLeave>(entity =>
        {
            entity.HasKey(l => l.LeaveId);

            entity.Property(l => l.Reason)
                .HasMaxLength(500);

            entity.Property(l => l.Status)
                .IsRequired()
                .HasMaxLength(30);


            // Doctor -> Leaves (1:M)
            entity.HasOne(l => l.Doctor)
                .WithMany(d => d.DoctorLeaves)
                .HasForeignKey(l => l.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);


            // End date must not be before start date
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_DoctorLeaves_DateRange",
                    "\"EndDate\" >= \"StartDate\""
                );
            });
        });


        // ============================================================
        // APPOINTMENT
        // ============================================================

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.AppointmentId);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(a => a.Symptoms)
                .HasMaxLength(2000);

            entity.Property(a => a.CreatedAt)
                .IsRequired();


            // Appointment -> Patient
            entity.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Appointment -> Doctor
            entity.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);


            // Appointment -> DoctorSchedule
            entity.HasOne(a => a.Schedule)
                .WithMany()
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);


            // Prevent double booking for the same doctor,
            // date and time.
            entity.HasIndex(a => new
            {
                a.DoctorId,
                a.AppointmentDate,
                a.AppointmentTime
            })
            .IsUnique();
        });


        // ============================================================
        // MEDICAL RECORD
        // ============================================================

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(r => r.RecordId);

            entity.Property(r => r.Diagnosis)
                .HasMaxLength(2000);

            entity.Property(r => r.Treatment)
                .HasMaxLength(2000);

            entity.Property(r => r.Notes)
                .HasMaxLength(5000);

            entity.Property(r => r.CreatedAt)
                .IsRequired();


            // Patient -> Medical Records
            entity.HasOne(r => r.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Doctor -> Medical Records
            entity.HasOne(r => r.Doctor)
                .WithMany(d => d.MedicalRecords)
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);


            // Appointment -> Medical Record (1:1)
            entity.HasOne(r => r.Appointment)
                .WithOne()
                .HasForeignKey<MedicalRecord>(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // One appointment should have only one medical record
            entity.HasIndex(r => r.AppointmentId)
                .IsUnique();
        });


        // ============================================================
        // PRESCRIPTION
        // ============================================================

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(p => p.PrescriptionId);

            entity.Property(p => p.Medicine)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Dosage)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Duration)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Frequency)
                .HasMaxLength(100);

            entity.Property(p => p.Instructions)
                .HasMaxLength(1000);

            entity.Property(p => p.CreatedAt)
                .IsRequired();


            // Medical Record -> Prescriptions (1:M)
            entity.HasOne(p => p.Record)
                .WithMany(r => r.Prescriptions)
                .HasForeignKey(p => p.RecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // LAB REPORT
        // ============================================================

        modelBuilder.Entity<LabReport>(entity =>
        {
            entity.HasKey(l => l.LabReportId);

            entity.Property(l => l.ReportName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(l => l.ReportType)
                .HasMaxLength(100);

            entity.Property(l => l.FilePath)
                .HasMaxLength(500);

            entity.Property(l => l.UploadedAt)
                .IsRequired();


            // Patient -> Lab Reports
            entity.HasOne(l => l.Patient)
                .WithMany()
                .HasForeignKey(l => l.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Medical Record -> Lab Reports
            entity.HasOne(l => l.Record)
                .WithMany(r => r.LabReports)
                .HasForeignKey(l => l.RecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================================================
        // BILL
        // ============================================================

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(b => b.BillId);

            entity.Property(b => b.TotalAmount)
                .HasPrecision(12, 2);

            entity.Property(b => b.BillStatus)
                .IsRequired()
                .HasMaxLength(30);


            // Appointment -> Bill (1:1)
            entity.HasOne(b => b.Appointment)
                .WithOne()
                .HasForeignKey<Bill>(b => b.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // One appointment = one bill
            entity.HasIndex(b => b.AppointmentId)
                .IsUnique();


            // Patient -> Bills
            entity.HasOne(b => b.Patient)
                .WithMany(p => p.Bills)
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Bill amount cannot be negative
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Bills_TotalAmount",
                    "\"TotalAmount\" >= 0"
                );
            });
        });


        // ============================================================
        // PAYMENT
        // ============================================================

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);

            entity.Property(p => p.Amount)
                .HasPrecision(12, 2);

            entity.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.PaymentStatus)
                .IsRequired()
                .HasMaxLength(30);


            // Bill -> Payments (1:M)
            entity.HasOne(p => p.Bill)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.Cascade);


            // Payment amount cannot be negative
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Payments_Amount",
                    "\"Amount\" >= 0"
                );
            });
        });


        // ============================================================
        // NOTIFICATION
        // ============================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.NotificationId);

            entity.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(n => n.NotificationType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(n => n.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(n => n.CreatedAt)
                .IsRequired();

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // PATIENT ALLERGY
        // ============================================================

        modelBuilder.Entity<PatientAllergy>(entity =>
        {
            entity.HasKey(a => a.AllergyId);

            entity.Property(a => a.AllergyName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(a => a.Reaction)
                .HasMaxLength(500);

            entity.Property(a => a.Severity)
                .HasMaxLength(50);

            entity.HasOne(a => a.Patient)
                .WithMany(p => p.Allergies)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================================================
        // CHRONIC CONDITION
        // ============================================================

        modelBuilder.Entity<ChronicCondition>(entity =>
        {
            entity.HasKey(c => c.ConditionId);

            entity.Property(c => c.ConditionName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            entity.Property(c => c.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasOne(c => c.Patient)
                .WithMany(p => p.ChronicConditions)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================================================
        // INSURANCE POLICY
        // ============================================================

        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.HasKey(i => i.InsurancePolicyId);

            entity.Property(i => i.ProviderName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(i => i.PolicyNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(i => i.CoverageAmount)
                .HasPrecision(12, 2);

            entity.Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(i => i.PolicyNumber)
                .IsUnique();

            entity.HasOne(i => i.Patient)
                .WithMany(p => p.InsurancePolicies)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_InsurancePolicies_DateRange",
                    "\"EndDate\" >= \"StartDate\""
                );

                t.HasCheckConstraint(
                    "CK_InsurancePolicies_CoverageAmount",
                    "\"CoverageAmount\" >= 0"
                );
            });
        });


        // ============================================================
        // INSURANCE CLAIM
        // ============================================================

        modelBuilder.Entity<InsuranceClaim>(entity =>
        {
            entity.HasKey(c => c.ClaimId);

            entity.Property(c => c.ClaimAmount)
                .HasPrecision(12, 2);

            entity.Property(c => c.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(c => c.ClaimDetails)
                .HasMaxLength(2000);

            entity.HasOne(c => c.InsurancePolicy)
                .WithMany(i => i.InsuranceClaims)
                .HasForeignKey(c => c.InsurancePolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Bill)
                .WithMany(b => b.InsuranceClaims)
                .HasForeignKey(c => c.BillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_InsuranceClaims_ClaimAmount",
                    "\"ClaimAmount\" >= 0"
                );
            });
        });


        // ============================================================
        // AI WORKFLOW
        // ============================================================

        modelBuilder.Entity<AIWorkflow>(entity =>
        {
            entity.HasKey(w => w.WorkflowId);

            entity.Property(w => w.WorkflowType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(w => w.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(w => w.UserRequest)
                .HasMaxLength(5000);

            entity.Property(w => w.StartedAt)
                .IsRequired();


            // Workflow -> Appointment
            entity.HasOne(w => w.Appointment)
                .WithMany()
                .HasForeignKey(w => w.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);


            // Workflow -> Patient
            entity.HasOne(w => w.Patient)
                .WithMany(p => p.AIWorkflows)
                .HasForeignKey(w => w.PatientId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // ============================================================
        // AI WORKFLOW STEP
        // ============================================================

        modelBuilder.Entity<AIWorkflowStep>(entity =>
        {
            entity.HasKey(s => s.StepId);

            entity.Property(s => s.AgentName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.InputData)
                .HasMaxLength(10000);

            entity.Property(s => s.OutputData)
                .HasMaxLength(10000);

            entity.Property(s => s.StepOrder)
                .IsRequired();

            entity.HasOne(s => s.Workflow)
                .WithMany(w => w.Steps)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new
            {
                s.WorkflowId,
                s.StepOrder
            })
            .IsUnique();
        });


        // ============================================================
        // AI RECOMMENDATION
        // ============================================================

        modelBuilder.Entity<AIRecommendation>(entity =>
        {
            entity.HasKey(r => r.RecommendationId);

            entity.Property(r => r.RecommendationType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(r => r.Recommendation)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(r => r.Reasoning)
                .HasMaxLength(10000);

            entity.Property(r => r.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(r => r.Workflow)
                .WithMany(w => w.Recommendations)
                .HasForeignKey(r => r.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // AI APPROVAL
        // ============================================================

        modelBuilder.Entity<AIApproval>(entity =>
        {
            entity.HasKey(a => a.ApprovalId);

            entity.Property(a => a.Decision)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Comments)
                .HasMaxLength(5000);

            entity.Property(a => a.RequestedAt)
                .IsRequired();


            // Workflow -> Approval
            entity.HasOne(a => a.Workflow)
                .WithMany(w => w.Approvals)
                .HasForeignKey(a => a.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);


            // User -> AI Approvals
            entity.HasOne(a => a.Approver)
                .WithMany(u => u.AIApprovals)
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });



    }


}