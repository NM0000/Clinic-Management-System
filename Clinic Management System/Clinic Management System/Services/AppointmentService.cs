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
        private const int SlotDurationMinutes = 30;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppointmentResponseDto> CreateAppointmentAsync(AppointmentCreateDto request)
        {
            // Validate future date
            if (request.AppointmentDate <= DateTime.Now)
                throw new ArgumentException("Appointment must be scheduled for a future date and time");

            // Validate patient exists
            var patient = await _context.Patients.FindAsync(request.PatientId);
            if (patient == null)
                throw new ArgumentException("Patient not found");

            // Validate doctor exists
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.DoctorId);
            if (doctor == null)
                throw new ArgumentException("Doctor not found");

            // Validate doctor has schedule for this day and time
            var dayOfWeek = request.AppointmentDate.DayOfWeek;
            var appointmentTime = request.AppointmentDate.TimeOfDay;

            var hasSchedule = await _context.DoctorSchedules
                .AnyAsync(s => s.DoctorId == request.DoctorId
                    && s.DayOfWeek == dayOfWeek
                    && s.StartTime <= appointmentTime
                    && s.EndTime >= appointmentTime.Add(TimeSpan.FromMinutes(SlotDurationMinutes)));

            if (!hasSchedule)
                throw new InvalidOperationException("Doctor is not available at the requested time");

            // Check for double-booking (same doctor, same time)
            var isBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == request.DoctorId
                    && a.AppointmentDate == request.AppointmentDate
                    && a.Status == AppointmentStatus.Scheduled);

            if (isBooked)
                throw new InvalidOperationException("This time slot is already booked");

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

            return await GetAppointmentByIdAsync(appointment.Id)
                ?? throw new Exception("Failed to retrieve created appointment");
        }

        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
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
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => MapToResponseDto(a))
                .ToListAsync();
        }

        public async Task<List<AppointmentResponseDto>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            return await _context.Appointments
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

            // Validate future date
            if (request.AppointmentDate <= DateTime.Now)
                throw new ArgumentException("Appointment must be scheduled for a future date and time");

            // If date/time changed, validate schedule and availability
            if (appointment.AppointmentDate != request.AppointmentDate)
            {
                var dayOfWeek = request.AppointmentDate.DayOfWeek;
                var appointmentTime = request.AppointmentDate.TimeOfDay;

                var hasSchedule = await _context.DoctorSchedules
                    .AnyAsync(s => s.DoctorId == appointment.DoctorId
                        && s.DayOfWeek == dayOfWeek
                        && s.StartTime <= appointmentTime
                        && s.EndTime >= appointmentTime.Add(TimeSpan.FromMinutes(SlotDurationMinutes)));

                if (!hasSchedule)
                    throw new InvalidOperationException("Doctor is not available at the requested time");

                // Check for double-booking (excluding current appointment)
                var isBooked = await _context.Appointments
                    .AnyAsync(a => a.Id != id
                        && a.DoctorId == appointment.DoctorId
                        && a.AppointmentDate == request.AppointmentDate
                        && a.Status == AppointmentStatus.Scheduled);

                if (isBooked)
                    throw new InvalidOperationException("This time slot is already booked");

                // Mark old status as rescheduled if date changed
                appointment.Status = AppointmentStatus.Rescheduled;
            }

            appointment.AppointmentDate = request.AppointmentDate;
            appointment.Notes = request.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponseDto(appointment);
        }

        public async Task<AppointmentResponseDto?> UpdateAppointmentStatusAsync(int id, AppointmentStatusUpdateDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return null;

            appointment.Status = request.Status;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponseDto(appointment);
        }

        public async Task<bool> CancelAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsCompletedAsync(int id, int doctorId)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            // Verify appointment belongs to this doctor
            if (appointment.DoctorId != doctorId)
                throw new UnauthorizedAccessException("You can only mark your own appointments as completed");

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

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