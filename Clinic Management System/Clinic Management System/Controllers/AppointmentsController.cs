using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Appointments;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ApplicationDbContext _context;

        public AppointmentsController(IAppointmentService appointmentService, ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _context = context;
        }

        // POST: api/Appointments
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentCreateDto request)
        {
            try
            {
                var appointment = await _appointmentService.CreateAppointmentAsync(request);
                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // GET: api/Appointments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }

        // GET: api/Appointments/my-appointments (For Doctors)
        [HttpGet("my-appointments")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetMyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Get doctor ID from userId
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound(new { message = "Doctor profile not found" });

            var appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctor.Id);
            return Ok(appointments);
        }

        // GET: api/Appointments/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            // If user is a doctor, verify they own this appointment
            if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

                if (doctor == null || appointment.DoctorId != doctor.Id)
                    return Forbid();
            }

            return Ok(appointment);
        }

        // GET: api/Appointments/patient/5
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetAppointmentsByPatient(int patientId)
        {
            var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patientId);
            return Ok(appointments);
        }

        // PUT: api/Appointments/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateDto request)
        {
            try
            {
                var appointment = await _appointmentService.UpdateAppointmentAsync(id, request);
                if (appointment == null)
                    return NotFound(new { message = "Appointment not found" });

                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // POST: api/Appointments/5/complete
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

                if (doctor == null)
                    return NotFound(new { message = "Doctor profile not found" });

                var result = await _appointmentService.MarkAsCompletedAsync(id, doctor.Id);
                if (!result)
                    return NotFound(new { message = "Appointment not found" });

                return Ok(new { message = "Appointment marked as completed" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // POST: api/Appointments/5/cancel
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id);
            if (!result)
                return NotFound(new { message = "Appointment not found" });

            return Ok(new { message = "Appointment cancelled successfully" });
        }

        // PUT: api/Appointments/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] AppointmentStatusUpdateDto request)
        {
            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, request);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            return Ok(appointment);
        }
    }
}