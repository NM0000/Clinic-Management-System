using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Schedules
{
    public class DoctorScheduleUpdateDto
    {
        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }
    }
}