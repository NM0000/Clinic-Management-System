using Clinic_Management_System.DTOs.Appointments;

namespace Clinic_Management_System.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentResponseDto> CreateAppointmentAsync(AppointmentCreateDto request);
        Task<AppointmentResponseDto?> GetAppointmentByIdAsync(int id);
        Task<List<AppointmentResponseDto>> GetAllAppointmentsAsync();
        Task<List<AppointmentResponseDto>> GetAppointmentsByDoctorIdAsync(int doctorId);
        Task<List<AppointmentResponseDto>> GetAppointmentsByUserIdAsync(string userId);
        Task<List<AppointmentResponseDto>> GetAppointmentsByPatientIdAsync(int patientId);
        Task<AppointmentResponseDto?> UpdateAppointmentAsync(int id, AppointmentUpdateDto request);
        Task<bool> MarkAsCompletedAsync(int id, string userId); 
        Task<bool> CancelAppointmentAsync(int id);
        Task<PagedResult<AppointmentResponseDto>> GetAppointmentsAdvancedAsync(
            AppointmentSearchDto searchDto,
            string? currentUserId,
            string? userRole);
    }
}