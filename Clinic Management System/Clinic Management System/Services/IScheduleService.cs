using Clinic_Management_System.DTOs.Schedules;

namespace Clinic_Management_System.Services
{
    public interface IScheduleService
    {
        Task<DoctorScheduleResponseDto> CreateScheduleAsync(DoctorScheduleCreateDto request);
        Task<List<DoctorScheduleResponseDto>> GetSchedulesByDoctorIdAsync(int doctorId);
        Task<DoctorScheduleResponseDto?> GetScheduleByIdAsync(int id);
        Task<DoctorScheduleResponseDto?> UpdateScheduleAsync(int id, DoctorScheduleUpdateDto request);
        Task<bool> DeleteScheduleAsync(int id);
        Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(AvailableSlotsRequestDto request);
    }
}