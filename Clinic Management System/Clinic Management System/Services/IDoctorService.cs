using Clinic_Management_System.DTOs.Doctors;

namespace Clinic_Management_System.Services
{
    public interface IDoctorService
    {
        Task<DoctorResponseDto> CreateDoctorAsync(DoctorCreateRequestDto request);
        Task<DoctorResponseDto?> GetDoctorByIdAsync(int id);
        Task<List<DoctorListResponseDto>> GetAllDoctorsAsync();
        Task<DoctorResponseDto?> UpdateDoctorAsync(int id, DoctorUpdateRequestDto request);
        Task<bool> DeleteDoctorAsync(int id);
    }
}
