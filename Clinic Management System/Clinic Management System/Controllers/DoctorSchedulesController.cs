using Clinic_Management_System.DTOs.Schedules;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoctorSchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public DoctorSchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        // POST: api/DoctorSchedules
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
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<ActionResult<List<DoctorScheduleResponseDto>>> GetSchedulesByDoctor(int doctorId)
        {
            var schedules = await _scheduleService.GetSchedulesByDoctorIdAsync(doctorId);
            return Ok(schedules);
        }

        // PUT: api/DoctorSchedules/5
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