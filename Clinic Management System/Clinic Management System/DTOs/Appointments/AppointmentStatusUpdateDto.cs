using Clinic_Management_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Appointments
{
    public class AppointmentStatusUpdateDto
    {
        [Required(ErrorMessage = "Status is required")]
        public AppointmentStatus Status { get; set; }
    }
}