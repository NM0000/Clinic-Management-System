using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Appointments;
using Clinic_Management_System.Models;
using Clinic_Management_System.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private const int SlotDurationMinutes = 30;

        public AppointmentService(
            ApplicationDbContext context,
            ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppointmentResponseDto> CreateAppointmentAsync(AppointmentCreateDto request)
        {
            _logger.LogInformation("Creating appointment for Patient {PatientId} with Doctor {DoctorId}",
                request.PatientId, request.DoctorId);

            await ValidateFutureDateAsync(request.AppointmentDate);
            await ValidatePatientExistsAsync(request.PatientId);
            await ValidateDoctorExistsAsync(request.DoctorId);
            await ValidateDoctorScheduleAsync(request.DoctorId, request.AppointmentDate);
            await ValidateNoDoubleBookingAsync(request.DoctorId, request.AppointmentDate);

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                AppointmentDate = request.AppointmentDate,
                Status = AppointmentStatus.Scheduled,
                Notes = request.Notes
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} created successfully", appointment.Id);

            return await GetAppointmentByIdAsync(appointment.Id)
                ?? throw new InvalidOperationException("Failed to retrieve created appointment");
        }

        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
                .AsNoTracking() 
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(a => a.Id == id)
                .Select(a => MapToResponseDto(a))
                .FirstOrDefaultAsync();
        }

        public async Task<List<AppointmentResponseDto>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .AsNoTracking() 
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => MapToResponseDto(a))
                .ToListAsync();
        }

        public async Task<List<AppointmentResponseDto>> GetAppointmentsByDoctorIdAsync(int doctorId)
        {
            return await _context.Appointments
                .AsNoTracking() 
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => MapToResponseDto(a))
                .ToListAsync();
        }

        //  NEW METHOD - Get appointments by UserId (for doctors)
        public async Task<List<AppointmentResponseDto>> GetAppointmentsByUserIdAsync(string userId)
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found");

            return await GetAppointmentsByDoctorIdAsync(doctor.Id);
        }

        public async Task<List<AppointmentResponseDto>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            return await _context.Appointments
                .AsNoTracking() 
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => MapToResponseDto(a))
                .ToListAsync();
        }

        public async Task<AppointmentResponseDto?> UpdateAppointmentAsync(int id, AppointmentUpdateDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return null;

            await ValidateFutureDateAsync(request.AppointmentDate);

            if (appointment.AppointmentDate != request.AppointmentDate)
            {
                await ValidateDoctorScheduleAsync(appointment.DoctorId, request.AppointmentDate);
                await ValidateNoDoubleBookingAsync(appointment.DoctorId, request.AppointmentDate, id);
                appointment.Status = AppointmentStatus.Rescheduled;
            }

            appointment.AppointmentDate = request.AppointmentDate;
            appointment.Notes = request.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} updated successfully", id);

            return MapToResponseDto(appointment);
        }

        public async Task<bool> MarkAsCompletedAsync(int id, string userId)
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            if (appointment.DoctorId != doctor.Id)
                throw new UnauthorizedAccessException("You can only mark your own appointments as completed");

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} marked as completed by Doctor {DoctorId}",
                id, doctor.Id);

            return true;
        }

        public async Task<bool> CancelAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} cancelled", id);

            return true;
        }

        //  PRIVATE VALIDATION METHODS - Keep business logic in service
        private async Task ValidateFutureDateAsync(DateTime appointmentDate)
        {
            if (appointmentDate <= DateTime.Now)
                throw new ArgumentException("Appointment must be scheduled for a future date and time");
        }

        private async Task ValidatePatientExistsAsync(int patientId)
        {
            var exists = await _context.Patients.AnyAsync(p => p.Id == patientId);
            if (!exists)
                throw new ArgumentException("Patient not found");
        }

        private async Task ValidateDoctorExistsAsync(int doctorId)
        {
            var exists = await _context.Doctors.AnyAsync(d => d.Id == doctorId);
            if (!exists)
                throw new ArgumentException("Doctor not found");
        }

        private async Task ValidateDoctorScheduleAsync(int doctorId, DateTime appointmentDate)
        {
            var dayOfWeek = appointmentDate.DayOfWeek;
            var appointmentTime = appointmentDate.TimeOfDay;

            var hasSchedule = await _context.DoctorSchedules
                .AsNoTracking()
                .AnyAsync(s => s.DoctorId == doctorId
                    && s.DayOfWeek == dayOfWeek
                    && s.StartTime <= appointmentTime
                    && s.EndTime >= appointmentTime.Add(TimeSpan.FromMinutes(SlotDurationMinutes)));

            if (!hasSchedule)
                throw new InvalidOperationException("Doctor is not available at the requested time");
        }

        private async Task ValidateNoDoubleBookingAsync(int doctorId, DateTime appointmentDate, int? excludeAppointmentId = null)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId
                    && a.AppointmentDate == appointmentDate
                    && a.Status == AppointmentStatus.Scheduled);

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId.Value);

            var isBooked = await query.AnyAsync();

            if (isBooked)
                throw new InvalidOperationException("This time slot is already booked");
        }

        //  PRIVATE MAPPING METHOD - Consistent DTO mapping
        private static AppointmentResponseDto MapToResponseDto(Appointment appointment)
        {
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = $"{appointment.Patient?.FirstName} {appointment.Patient?.LastName}",
                PatientEmail = appointment.Patient?.Email ?? string.Empty,
                PatientPhone = appointment.Patient?.PhoneNumber ?? string.Empty,
                DoctorId = appointment.DoctorId,
                DoctorName = appointment.Doctor?.User?.FullName ?? string.Empty,
                DoctorSpecialization = appointment.Doctor?.Specialization ?? string.Empty,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };
        }
    }
}