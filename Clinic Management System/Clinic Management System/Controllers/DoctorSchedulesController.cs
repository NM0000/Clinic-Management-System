using Clinic_Management_System.DTOs.Schedules;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    /// <summary>
    /// Controller exposing API endpoints for managing doctor schedules and available slots.
    /// Requires authentication; individual actions enforce role-based access.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoctorSchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoctorSchedulesController"/> class.
        /// </summary>
        /// <param name="scheduleService">Service used to manage doctor schedules (<see cref="IScheduleService"/>).</param>
        public DoctorSchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        // POST: api/DoctorSchedules
        /// <summary>
        /// Creates a new doctor schedule.
        /// </summary>
        /// <param name="request">The schedule creation DTO (<see cref="DoctorScheduleCreateDto"/>).</param>
        /// <returns>
        /// 201 Created with the created schedule on success;
        /// 400 BadRequest for invalid input; 409 Conflict for business rule violations.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSchedule([FromBody] DoctorScheduleCreateDto request)
        {
            try
            {
                var schedule = await _scheduleService.CreateScheduleAsync(request);
                return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
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

        // GET: api/DoctorSchedules/5
        /// <summary>
        /// Retrieves a schedule by identifier.
        /// </summary>
        /// <param name="id">Schedule identifier.</param>
        /// <returns>200 OK with <see cref="DoctorScheduleResponseDto"/> when found; 404 NotFound otherwise.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<DoctorScheduleResponseDto>> GetSchedule(int id)
        {
            var schedule = await _scheduleService.GetScheduleByIdAsync(id);
            if (schedule == null)
                return NotFound(new { message = "Schedule not found" });

            return Ok(schedule);
        }

        // GET: api/DoctorSchedules/doctor/5
        /// <summary>
        /// Retrieves schedules for a specified doctor.
        /// </summary>
        /// <param name="doctorId">Doctor identifier.</param>
        /// <returns>200 OK with a list of <see cref="DoctorScheduleResponseDto"/>.</returns>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<DoctorScheduleResponseDto>>> GetSchedulesByDoctor(int doctorId)
        {
            var schedules = await _scheduleService.GetSchedulesByDoctorIdAsync(doctorId);
            return Ok(schedules);
        }

        // PUT: api/DoctorSchedules/5
        /// <summary>
        /// Updates an existing schedule.
        /// </summary>
        /// <param name="id">Schedule identifier.</param>
        /// <param name="request">The schedule update DTO (<see cref="DoctorScheduleUpdateDto"/>).</param>
        /// <returns>200 OK with the updated schedule; 404 NotFound if not found; 400/409 on validation/business errors.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] DoctorScheduleUpdateDto request)
        {
            try
            {
                var schedule = await _scheduleService.UpdateScheduleAsync(id, request);
                if (schedule == null)
                    return NotFound(new { message = "Schedule not found" });

                return Ok(schedule);
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

        // DELETE: api/DoctorSchedules/5
        /// <summary>
        /// Deletes a schedule by identifier.
        /// </summary>
        /// <param name="id">Schedule identifier.</param>
        /// <returns>200 OK on success; 404 NotFound when the schedule does not exist.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var result = await _scheduleService.DeleteScheduleAsync(id);
            if (!result)
                return NotFound(new { message = "Schedule not found" });

            return Ok(new { message = "Schedule deleted successfully" });
        }

        // POST: api/DoctorSchedules/available-slots
        /// <summary>
        /// Returns available appointment slots for a doctor in a given date/time range.
        /// </summary>
        /// <param name="request">Request DTO describing the doctor and date range (<see cref="AvailableSlotsRequestDto"/>).</param>
        /// <returns>200 OK with <see cref="AvailableSlotsResponseDto"/> describing available slots; 400 BadRequest for invalid requests.</returns>
        [HttpPost("available-slots")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<AvailableSlotsResponseDto>> GetAvailableSlots([FromBody] AvailableSlotsRequestDto request)
        {
            try
            {
                var slots = await _scheduleService.GetAvailableSlotsAsync(request);
                return Ok(slots);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}