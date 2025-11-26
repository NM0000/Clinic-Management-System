using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Appointments
{
    public class AppointmentUpdateDto
    {
        [Required(ErrorMessage = "Appointment date is required")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}