using Clinic_Management_System.DTOs.Appointments;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new appointment
        /// </summary>
        /// <param name="request">Appointment details</param>
        /// <returns>Created appointment</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentCreateDto request)
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(request);
            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
        }

        /// <summary>
        /// Gets all appointments (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }

        /// <summary>
        /// Gets appointments for the logged-in doctor
        /// </summary>
        [HttpGet("my-appointments")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetMyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var appointments = await _appointmentService.GetAppointmentsByUserIdAsync(userId);
            return Ok(appointments);
        }

        /// <summary>
        /// Gets a single appointment by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            // Doctor authorization check
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctorAppointments = await _appointmentService.GetAppointmentsByUserIdAsync(userId!);

                if (!doctorAppointments.Any(a => a.Id == id))
                    return Forbid();
            }

            return Ok(appointment);
        }

        /// <summary>
        /// Gets appointments for a specific patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Receptionist")]
        [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetAppointmentsByPatient(int patientId)
        {
            var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patientId);
            return Ok(appointments);
        }

        /// <summary>
        /// Updates an existing appointment
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Receptionist")]
        [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateDto request)
        {
            var appointment = await _appointmentService.UpdateAppointmentAsync(id, request);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            return Ok(appointment);
        }

        /// <summary>
        /// Marks an appointment as completed (Doctor only)
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _appointmentService.MarkAsCompletedAsync(id, userId);
            if (!result)
                return NotFound(new { message = "Appointment not found" });

            return Ok(new { message = "Appointment marked as completed" });
        }

        /// <summary>
        /// Cancels an appointment
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Receptionist")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id);
            if (!result)
                return NotFound(new { message = "Appointment not found" });

            return Ok(new { message = "Appointment cancelled successfully" });
        }
    }
}