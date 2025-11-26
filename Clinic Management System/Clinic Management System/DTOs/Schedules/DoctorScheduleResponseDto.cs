namespace Clinic_Management_System.DTOs.Schedules
{
    public class DoctorScheduleResponseDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public DayOfWeek DayOfWeek { get; set; }
        public string DayName => DayOfWeek.ToString();
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string StartTimeFormatted => StartTime.ToString(@"hh\:mm");
        public string EndTimeFormatted => EndTime.ToString(@"hh\:mm");
    }
}