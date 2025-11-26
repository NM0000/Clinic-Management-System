namespace Clinic_Management_System.DTOs.Schedules
{
    public class AvailableSlotsResponseDto
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public List<TimeSlotDto> AvailableSlots { get; set; } = new();
    }

    public class TimeSlotDto
    {
        public DateTime SlotDateTime { get; set; }
        public string TimeFormatted { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}