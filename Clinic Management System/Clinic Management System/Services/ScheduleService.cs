using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Schedules;
using Clinic_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;
        private const int SlotDurationMinutes = 30;

        public ScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DoctorScheduleResponseDto> CreateScheduleAsync(DoctorScheduleCreateDto request)
        {
            // Validate doctor exists
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.DoctorId);
            if (doctor == null)
                throw new ArgumentException("Doctor not found");

            // Validate time range
            if (request.StartTime >= request.EndTime)
                throw new ArgumentException("Start time must be before end time");

            // Check for overlapping schedules
            var overlapping = await _context.DoctorSchedules
                .AnyAsync(s => s.DoctorId == request.DoctorId
                    && s.DayOfWeek == request.DayOfWeek
                    && ((request.StartTime >= s.StartTime && request.StartTime < s.EndTime)
                        || (request.EndTime > s.StartTime && request.EndTime <= s.EndTime)
                        || (request.StartTime <= s.StartTime && request.EndTime >= s.EndTime)));

            if (overlapping)
                throw new InvalidOperationException("Schedule overlaps with existing schedule for this day");

            var schedule = new DoctorSchedule
            {
                DoctorId = request.DoctorId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            _context.DoctorSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return MapToResponseDto(schedule, doctor);
        }

        public async Task<List<DoctorScheduleResponseDto>> GetSchedulesByDoctorIdAsync(int doctorId)
        {
            return await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d!.User)
                .Where(s => s.DoctorId == doctorId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => new DoctorScheduleResponseDto
                {
                    Id = s.Id,
                    DoctorId = s.DoctorId,
                    DoctorName = s.Doctor!.User!.FullName,
                    Specialization = s.Doctor.Specialization,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToListAsync();
        }

        public async Task<DoctorScheduleResponseDto?> GetScheduleByIdAsync(int id)
        {
            var schedule = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            return schedule == null ? null : MapToResponseDto(schedule, schedule.Doctor!);
        }

        public async Task<DoctorScheduleResponseDto?> UpdateScheduleAsync(int id, DoctorScheduleUpdateDto request)
        {
            var schedule = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
                return null;

            // Validate time range
            if (request.StartTime >= request.EndTime)
                throw new ArgumentException("Start time must be before end time");

            // Check for overlapping schedules (excluding current)
            var overlapping = await _context.DoctorSchedules
                .AnyAsync(s => s.Id != id
                    && s.DoctorId == schedule.DoctorId
                    && s.DayOfWeek == schedule.DayOfWeek
                    && ((request.StartTime >= s.StartTime && request.StartTime < s.EndTime)
                        || (request.EndTime > s.StartTime && request.EndTime <= s.EndTime)
                        || (request.StartTime <= s.StartTime && request.EndTime >= s.EndTime)));

            if (overlapping)
                throw new InvalidOperationException("Schedule overlaps with existing schedule for this day");

            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;

            await _context.SaveChangesAsync();

            return MapToResponseDto(schedule, schedule.Doctor!);
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.DoctorSchedules.FindAsync(id);
            if (schedule == null)
                return false;

            _context.DoctorSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(AvailableSlotsRequestDto request)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

            if (doctor == null)
                throw new ArgumentException("Doctor not found");

            var dayOfWeek = request.Date.DayOfWeek;

            // Get doctor's schedules for this day
            var schedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == request.DoctorId && s.DayOfWeek == dayOfWeek)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!schedules.Any())
            {
                return new AvailableSlotsResponseDto
                {
                    DoctorId = request.DoctorId,
                    DoctorName = doctor.User?.FullName ?? string.Empty,
                    Date = request.Date.Date,
                    DayOfWeek = dayOfWeek.ToString(),
                    AvailableSlots = new List<TimeSlotDto>()
                };
            }

            // Get all appointments for this doctor on this date
            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == request.DoctorId
                    && a.AppointmentDate.Date == request.Date.Date
                    && a.Status == Models.Enums.AppointmentStatus.Scheduled)
                .Select(a => a.AppointmentDate)
                .ToListAsync();

            var bookedTimes = new HashSet<DateTime>(appointments);

            // Generate time slots
            var slots = new List<TimeSlotDto>();

            foreach (var schedule in schedules)
            {
                var currentTime = schedule.StartTime;

                while (currentTime.Add(TimeSpan.FromMinutes(SlotDurationMinutes)) <= schedule.EndTime)
                {
                    var slotDateTime = request.Date.Date.Add(currentTime);

                    slots.Add(new TimeSlotDto
                    {
                        SlotDateTime = slotDateTime,
                        TimeFormatted = slotDateTime.ToString("hh:mm tt"),
                        IsAvailable = !bookedTimes.Contains(slotDateTime)
                    });

                    currentTime = currentTime.Add(TimeSpan.FromMinutes(SlotDurationMinutes));
                }
            }

            return new AvailableSlotsResponseDto
            {
                DoctorId = request.DoctorId,
                DoctorName = doctor.User?.FullName ?? string.Empty,
                Date = request.Date.Date,
                DayOfWeek = dayOfWeek.ToString(),
                AvailableSlots = slots
            };
        }

        private static DoctorScheduleResponseDto MapToResponseDto(DoctorSchedule schedule, Doctor doctor)
        {
            return new DoctorScheduleResponseDto
            {
                Id = schedule.Id,
                DoctorId = schedule.DoctorId,
                DoctorName = doctor.User?.FullName ?? string.Empty,
                Specialization = doctor.Specialization,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime
            };
        }
    }
}