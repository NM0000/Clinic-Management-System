using Clinic_Management_System.DTOs.Patients;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // POST: api/Patients
        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CreatePatient([FromBody] PatientCreateRequestDto request)
        {
            try
            {
                var patient = await _patientService.CreatePatientAsync(request);
                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // GET: api/Patients
        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<ActionResult<List<PatientListResponseDto>>> GetAllPatients()
        {
            // TODO Phase 3: Doctors should only see patients linked to their appointments
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<ActionResult<PatientResponseDto>> GetPatient(int id)
        {
            // If user is a doctor, verify they have appointments with this patient
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var canAccess = await _patientService.CanDoctorAccessPatientAsync(id, userId);
                if (!canAccess)
                    return Forbid(); // 403 Forbidden - Doctor has no appointments with this patient
            }

            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
                return NotFound(new { message = "Patient not found" });

            return Ok(patient);
        }

        // PUT: api/Patients/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientUpdateRequestDto request)
        {
            try
            {
                var patient = await _patientService.UpdatePatientAsync(id, request);
                if (patient == null)
                    return NotFound(new { message = "Patient not found" });

                return Ok(patient);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE: api/Patients/5 (Soft Delete)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var result = await _patientService.SoftDeletePatientAsync(id);
            if (!result)
                return NotFound(new { message = "Patient not found" });

            return Ok(new { message = "Patient deleted successfully" });
        }

        // POST: api/Patients/5/restore
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestorePatient(int id)
        {
            var result = await _patientService.RestorePatientAsync(id);
            if (!result)
                return NotFound(new { message = "Patient not found or not deleted" });

            return Ok(new { message = "Patient restored successfully" });
        }
    }
}