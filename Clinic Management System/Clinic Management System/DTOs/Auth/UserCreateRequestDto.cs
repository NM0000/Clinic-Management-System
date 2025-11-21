using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Auth
{
    public class UserCreateRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role name is required")]
        [RegularExpression("^(Doctor|Receptionist)$", ErrorMessage = "Role must be either 'Doctor' or 'Receptionist'")]
        public string RoleName { get; set; } = string.Empty;

        // Doctor-specific fields (only used if RoleName = "Doctor")
        [StringLength(100)]
        public string? Specialization { get; set; }

        [StringLength(50)]
        public string? LicenseNumber { get; set; }
    }
}