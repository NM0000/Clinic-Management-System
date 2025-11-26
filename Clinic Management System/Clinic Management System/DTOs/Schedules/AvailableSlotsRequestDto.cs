using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Schedules
{
    public class AvailableSlotsRequestDto
    {
        [Required(ErrorMessage = "Doctor ID is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }
    }
}