using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Doctors
{
    public class DoctorCreateRequestDto
    {
        [Required(ErrorMessage = "User ID is required")]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Specialization must be between 2 and 100 characters")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "License number must be between 3 and 50 characters")]
        public string LicenseNumber { get; set; } = string.Empty;
    }
}
