using Clinic_Management_System.Models.Enums;

namespace Clinic_Management_System.DTOs.Appointments
{
    public class AppointmentSearchDto
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string? SortBy { get; set; } // "date", "patient", "doctor", "status"
        public bool IsDescending { get; set; } = false;

        // Filters
        public string? SearchTerm { get; set; } // Patient Name, Doctor Name, or Notes
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public AppointmentStatus? Status { get; set; }
        public string? Specialization { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool UpcomingOnly { get; set; } = false;
    }

    // Generic Wrapper for Pagination Results
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}