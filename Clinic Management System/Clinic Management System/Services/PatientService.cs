using Microsoft.EntityFrameworkCore;
using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Patients;
using Clinic_Management_System.Models;

namespace Clinic_Management_System.Services
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PatientResponseDto> CreatePatientAsync(PatientCreateRequestDto request)
        {
            // Check for duplicate email
            var existingPatient = await _context.Patients
                .IgnoreQueryFilters() // Check even soft-deleted patients
                .FirstOrDefaultAsync(p => p.Email == request.Email);

            if (existingPatient != null && !existingPatient.IsDeleted)
                throw new InvalidOperationException("A patient with this email already exists");

            var patient = new Patient
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return MapToResponseDto(patient);
        }

        public async Task<PatientResponseDto?> GetPatientByIdAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            return patient == null ? null : MapToResponseDto(patient);
        }

        public async Task<List<PatientListResponseDto>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Select(p => new PatientListResponseDto
                {
                    Id = p.Id,
                    FullName = $"{p.FirstName} {p.LastName}",
                    Age = DateTime.Now.Year - p.DateOfBirth.Year,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    Email = p.Email
                })
                .ToListAsync();
        }

        public async Task<PatientResponseDto?> UpdatePatientAsync(int id, PatientUpdateRequestDto request)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return null;

            // Check for duplicate email (excluding current patient)
            var duplicateEmail = await _context.Patients
                .AnyAsync(p => p.Email == request.Email && p.Id != id);

            if (duplicateEmail)
                throw new InvalidOperationException("Another patient with this email already exists");

            patient.FirstName = request.FirstName;
            patient.LastName = request.LastName;
            patient.DateOfBirth = request.DateOfBirth;
            patient.Gender = request.Gender;
            patient.PhoneNumber = request.PhoneNumber;
            patient.Email = request.Email;
            patient.Address = request.Address;

            await _context.SaveChangesAsync();

            return MapToResponseDto(patient);
        }

        public async Task<bool> SoftDeletePatientAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return false;

            patient.IsDeleted = true;
            patient.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestorePatientAsync(int id)
        {
            var patient = await _context.Patients
                .IgnoreQueryFilters() // Include soft-deleted patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null || !patient.IsDeleted)
                return false;

            patient.IsDeleted = false;
            patient.DeletedAt = null;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> CanDoctorAccessPatientAsync(int patientId, string userId)
        {
            // Get doctor ID from userId
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return false;

            // Check if doctor has any appointments with this patient
            var hasAppointment = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId && a.DoctorId == doctor.Id);

            return hasAppointment;
        }

        private static PatientResponseDto MapToResponseDto(Patient patient)
        {
            return new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email,
                Address = patient.Address,
                CreatedAt = patient.CreatedAt,
                IsDeleted = patient.IsDeleted
            };
        }
    }
}
