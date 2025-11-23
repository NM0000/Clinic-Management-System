using Clinic_Management_System.DTOs.Doctors;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        // POST: api/Doctors
        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] DoctorCreateRequestDto request)
        {
            try
            {
                var doctor = await _doctorService.CreateDoctorAsync(request);
                return CreatedAtAction(nameof(GetDoctor), new { id = doctor.Id }, doctor);
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

        // GET: api/Doctors
        [HttpGet]
        public async Task<ActionResult<List<DoctorListResponseDto>>> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        // GET: api/Doctors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorResponseDto>> GetDoctor(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound(new { message = "Doctor not found" });

            return Ok(doctor);
        }

        // PUT: api/Doctors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] DoctorUpdateRequestDto request)
        {
            var doctor = await _doctorService.UpdateDoctorAsync(id, request);
            if (doctor == null)
                return NotFound(new { message = "Doctor not found" });

            return Ok(doctor);
        }

        // DELETE: api/Doctors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var result = await _doctorService.DeleteDoctorAsync(id);
            if (!result)
                return NotFound(new { message = "Doctor not found" });

            return Ok(new { message = "Doctor deleted successfully" });
        }
    }
}