using Microsoft.EntityFrameworkCore;
using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Doctors;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Identity;

namespace Clinic_Management_System.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DoctorService(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<DoctorResponseDto> CreateDoctorAsync(DoctorCreateRequestDto request)
        {
            // Verify user exists and has Doctor role
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Doctor"))
                throw new ArgumentException("User must have Doctor role");

            // Check if doctor profile already exists
            var existingDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == request.UserId);

            if (existingDoctor != null)
                throw new InvalidOperationException("Doctor profile already exists for this user");

            // Create doctor
            var doctor = new Doctor
            {
                UserId = request.UserId,
                Specialization = request.Specialization,
                LicenseNumber = request.LicenseNumber
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            return await GetDoctorByIdAsync(doctor.Id)
                ?? throw new Exception("Failed to retrieve created doctor");
        }

        public async Task<DoctorResponseDto?> GetDoctorByIdAsync(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return null;

            return new DoctorResponseDto
            {
                Id = doctor.Id,
                UserId = doctor.UserId,
                FullName = doctor.User?.FullName ?? string.Empty,
                Email = doctor.User?.Email ?? string.Empty,
                PhoneNumber = doctor.User?.PhoneNumber ?? string.Empty,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                CreatedAt = doctor.CreatedAt
            };
        }

        public async Task<List<DoctorListResponseDto>> GetAllDoctorsAsync()
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Select(d => new DoctorListResponseDto
                {
                    Id = d.Id,
                    FullName = d.User!.FullName,
                    Email = d.User.Email!,
                    Specialization = d.Specialization,
                    LicenseNumber = d.LicenseNumber
                })
                .ToListAsync();
        }

        public async Task<DoctorResponseDto?> UpdateDoctorAsync(int id, DoctorUpdateRequestDto request)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return null;

            // Update doctor fields
            doctor.Specialization = request.Specialization;
            doctor.LicenseNumber = request.LicenseNumber;

            // Update user phone number if provided
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && doctor.User != null)
            {
                doctor.User.PhoneNumber = request.PhoneNumber;
            }

            await _context.SaveChangesAsync();

            return await GetDoctorByIdAsync(id);
        }

        public async Task<bool> DeleteDoctorAsync(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return false;

            // Check if doctor has any appointments
            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.DoctorId == id);

            if (hasAppointments)
                throw new InvalidOperationException("Cannot delete doctor with existing appointments. Please reassign or cancel appointments first.");

            // Store userId before deleting doctor
            var userId = doctor.UserId;

            // Delete doctor profile
            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            // Delete associated user account
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
            }

            return true;
        }
    }
}
