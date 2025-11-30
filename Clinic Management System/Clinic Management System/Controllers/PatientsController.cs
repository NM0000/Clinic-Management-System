using Clinic_Management_System.DTOs.Patients;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    /// <summary>
    /// API endpoints for creating, retrieving, updating, deleting and restoring patients.
    /// Requires authentication; individual actions enforce role-based access.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientsController"/> class.
        /// </summary>
        /// <param name="patientService">Service used to manage patient records (<see cref="IPatientService"/>).</param>
        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        /// <summary>
        /// Creates a new patient record.
        /// </summary>
        /// <param name="request">The patient creation request DTO (<see cref="PatientCreateRequestDto"/>).</param>
        /// <returns>
        /// 201 Created with the created <see cref="PatientResponseDto"/> when successful;
        /// 409 Conflict when a patient already exists.
        /// </returns>
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

        /// <summary>
        /// Retrieves a list of all patients.
        /// </summary>
        /// <returns>200 OK with a list of <see cref="PatientListResponseDto"/>.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<ActionResult<List<PatientListResponseDto>>> GetAllPatients()
        {
            // TODO Phase 3: Doctors should only see patients linked to their appointments
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        /// <summary>
        /// Retrieves a patient by identifier.
        /// If the caller is a Doctor (non-Admin), access is validated against the doctor's appointments.
        /// </summary>
        /// <param name="id">Patient identifier.</param>
        /// <returns>
        /// 200 OK with <see cref="PatientResponseDto"/> when found;
        /// 401 Unauthorized if user id claim is missing;
        /// 403 Forbidden if a doctor has no access; 
        /// 404 NotFound when the patient does not exist.
        /// </returns>
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

        /// <summary>
        /// Updates an existing patient.
        /// </summary>
        /// <param name="id">Patient identifier.</param>
        /// <param name="request">The update request DTO (<see cref="PatientUpdateRequestDto"/>).</param>
        /// <returns>
        /// 200 OK with the updated patient when successful;
        /// 404 NotFound when the patient does not exist;
        /// 409 Conflict on business rule violations.
        /// </returns>
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

        /// <summary>
        /// Soft-deletes a patient by id.
        /// </summary>
        /// <param name="id">Patient identifier.</param>
        /// <returns>200 OK on success; 404 NotFound when the patient does not exist.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var result = await _patientService.SoftDeletePatientAsync(id);
            if (!result)
                return NotFound(new { message = "Patient not found" });

            return Ok(new { message = "Patient deleted successfully" });
        }

        /// <summary>
        /// Restores a previously soft-deleted patient.
        /// </summary>
        /// <param name="id">Patient identifier.</param>
        /// <returns>200 OK on success; 404 NotFound when the patient does not exist or is not deleted.</returns>
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