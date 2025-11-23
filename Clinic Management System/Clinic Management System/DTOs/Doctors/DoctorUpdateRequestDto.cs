using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Doctors
{
    public class DoctorUpdateRequestDto
    {
        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Specialization must be between 2 and 100 characters")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "License number must be between 3 and 50 characters")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
