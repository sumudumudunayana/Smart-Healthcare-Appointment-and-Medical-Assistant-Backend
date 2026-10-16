using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.Patients;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Services
{
    public class PatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public PatientService(
            ApplicationDbContext context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        // =========================================================
        // CREATE PATIENT
        // =========================================================

        public async Task<PatientResponse> CreateAsync(
            CreatePatientRequest request)
        {
            string fullName = request.FullName?.Trim() ?? string.Empty;
            string email = request.Email?.Trim().ToLowerInvariant()
                           ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Email is required.");
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException(
                    "Please provide a valid email address.");
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                throw new InvalidOperationException(
                    "A user with this email already exists.");
            }

            // Find the Patient role.
            var patientRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Patient");

            if (patientRole == null)
            {
                throw new InvalidOperationException(
                    "Patient role was not found.");
            }

            // Create the user account.
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = _passwordService.HashPassword(
                    request.Password),
                Phone = EmptyToNull(request.Phone),
                RoleId = patientRole.RoleId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            // Create the patient profile.
            var patient = new Patient
            {
                PatientId = Guid.NewGuid(),
                UserId = user.UserId,
                DateOfBirth = request.DateOfBirth,
                Gender = EmptyToNull(request.Gender),
                BloodGroup = EmptyToNull(request.BloodGroup),
                Address = EmptyToNull(request.Address),
                EmergencyContact = EmptyToNull(
                    request.EmergencyContact)
            };

            _context.Users.Add(user);
            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            return Map(patient, user);
        }

        // =========================================================
        // GET ALL PATIENTS
        // =========================================================

        public async Task<List<PatientResponse>> GetAllAsync()
        {
            var patients = await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            return patients
                .Where(p => p.User != null)
                .Select(p => Map(p, p.User!))
                .ToList();
        }

        // =========================================================
        // GET PATIENT BY ID
        // =========================================================

        public async Task<PatientResponse?> GetByIdAsync(
            Guid patientId)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.PatientId == patientId);

            if (patient?.User == null)
            {
                return null;
            }

            return Map(patient, patient.User);
        }

        // =========================================================
        // UPDATE PATIENT
        // =========================================================

        public async Task<PatientResponse?> UpdateAsync(
            Guid patientId,
            UpdatePatientRequest request)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.PatientId == patientId);

            if (patient?.User == null)
            {
                return null;
            }

            string fullName = request.FullName?.Trim()
                              ?? string.Empty;

            string email = request.Email?.Trim().ToLowerInvariant()
                           ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Email is required.");
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException(
                    "Please provide a valid email address.");
            }

            // Prevent another user from using the same email.
            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != patient.UserId &&
                    u.Email.ToLower() == email);

            if (emailExists)
            {
                throw new InvalidOperationException(
                    "A user with this email already exists.");
            }

            // Update user information.
            patient.User.FullName = fullName;
            patient.User.Email = email;
            patient.User.Phone = EmptyToNull(request.Phone);

            // Update patient profile information.
            patient.DateOfBirth = request.DateOfBirth;
            patient.Gender = EmptyToNull(request.Gender);
            patient.BloodGroup = EmptyToNull(request.BloodGroup);
            patient.Address = EmptyToNull(request.Address);
            patient.EmergencyContact = EmptyToNull(
                request.EmergencyContact);

            await _context.SaveChangesAsync();

            return Map(patient, patient.User);
        }

        // =========================================================
        // DEACTIVATE PATIENT
        // =========================================================

        public async Task<bool> DeactivateAsync(
            Guid patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.PatientId == patientId);

            if (patient?.User == null)
            {
                return false;
            }

            if (string.Equals(
                    patient.User.Status,
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This patient account is already inactive.");
            }

            // Deactivate the account instead of deleting the patient.
            // This preserves medical records and appointment history.
            patient.User.Status = "Inactive";

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================================================
        // HELPER: NORMALIZE OPTIONAL STRING VALUES
        // =========================================================

        private static string? EmptyToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        // =========================================================
        // HELPER: VALIDATE EMAIL
        // =========================================================

        private static bool IsValidEmail(string email)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(email);

                return string.Equals(
                    address.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // =========================================================
        // HELPER: MAP PATIENT TO RESPONSE DTO
        // =========================================================

        private static PatientResponse Map(
    Patient patient,
    User user)
        {
            return new PatientResponse
            {
                PatientId = patient.PatientId,
                UserId = user.UserId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status,
                CreatedAt = user.CreatedAt,

                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                EmergencyContact = patient.EmergencyContact
            };
        }

        public async Task<PatientResponse?> GetMyProfileAsync(Guid userId)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient?.User == null)
            {
                return null;
            }

            return Map(patient, patient.User);
        }


        public async Task<PatientResponse?> UpdateMyProfileAsync(
         Guid userId,
         UpdatePatientRequest request)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient?.User == null)
            {
                return null;
            }

            // Reuse the existing update implementation.
            return await UpdateAsync(patient.PatientId, request);
        }
    }


}