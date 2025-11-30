using Clinic_Management_System.DTOs.Doctors;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    /// <summary>
    /// API endpoints for creating, retrieving, updating and deleting doctors.
    /// Requires the caller to be in the "Admin" role.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoctorsController"/> class.
        /// </summary>
        /// <param name="doctorService">Service used to manage doctors (<see cref="IDoctorService"/>).</param>
        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        // POST: api/Doctors
        /// <summary>
        /// Creates a new doctor.
        /// </summary>
        /// <param name="request">Request DTO containing doctor creation data (<see cref="DoctorCreateRequestDto"/>).</param>
        /// <returns>
        /// 201 Created with the created <see cref="DoctorResponseDto"/> on success;
        /// 400 BadRequest for invalid input; 409 Conflict for business rule violations.
        /// </returns>
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
        /// <summary>
        /// Retrieves all doctors.
        /// </summary>
        /// <returns>200 OK with a list of <see cref="DoctorListResponseDto"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<List<DoctorListResponseDto>>> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        // GET: api/Doctors/5
        /// <summary>
        /// Retrieves a doctor by identifier.
        /// </summary>
        /// <param name="id">Doctor identifier.</param>
        /// <returns>200 OK with <see cref="DoctorResponseDto"/> when found; 404 NotFound otherwise.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorResponseDto>> GetDoctor(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound(new { message = "Doctor not found" });

            return Ok(doctor);
        }

        // PUT: api/Doctors/5
        /// <summary>
        /// Updates an existing doctor.
        /// </summary>
        /// <param name="id">Doctor identifier.</param>
        /// <param name="request">Update DTO (<see cref="DoctorUpdateRequestDto"/>).</param>
        /// <returns>200 OK with the updated doctor; 404 NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] DoctorUpdateRequestDto request)
        {
            var doctor = await _doctorService.UpdateDoctorAsync(id, request);
            if (doctor == null)
                return NotFound(new { message = "Doctor not found" });

            return Ok(doctor);
        }

        // DELETE: api/Doctors/5
        /// <summary>
        /// Deletes a doctor by identifier.
        /// </summary>
        /// <param name="id">Doctor identifier.</param>
        /// <returns>200 OK on success; 404 NotFound when the doctor does not exist.</returns>
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