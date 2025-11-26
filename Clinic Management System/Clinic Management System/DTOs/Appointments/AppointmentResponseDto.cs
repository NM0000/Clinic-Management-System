using Clinic_Management_System.Models.Enums;

namespace Clinic_Management_System.DTOs.Appointments
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string AppointmentDateFormatted => AppointmentDate.ToString("yyyy-MM-dd hh:mm tt");
        public AppointmentStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}