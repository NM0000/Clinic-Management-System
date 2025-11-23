using Clinic_Management_System.DTOs.Patients;

namespace Clinic_Management_System.Services
{
    public interface IPatientService
    {
        Task<PatientResponseDto> CreatePatientAsync(PatientCreateRequestDto request);
        Task<PatientResponseDto?> GetPatientByIdAsync(int id);
        Task<List<PatientListResponseDto>> GetAllPatientsAsync();
        Task<PatientResponseDto?> UpdatePatientAsync(int id, PatientUpdateRequestDto request);
        Task<bool> SoftDeletePatientAsync(int id);
        Task<bool> RestorePatientAsync(int id);
    }
}
