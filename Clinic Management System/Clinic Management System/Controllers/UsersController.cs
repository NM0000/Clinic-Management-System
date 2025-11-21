using Clinic_Management_System.Data;
using Clinic_Management_System.DTOs.Auth;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequestDto request)
        {
            // Validate role
            if (request.RoleName != "Doctor" && request.RoleName != "Receptionist")
            {
                return BadRequest(new { message = "Invalid role. Must be 'Doctor' or 'Receptionist'" });
            }

            // Validate Doctor-specific fields
            if (request.RoleName == "Doctor")
            {
                if (string.IsNullOrWhiteSpace(request.Specialization) ||
                    string.IsNullOrWhiteSpace(request.LicenseNumber))
                {
                    return BadRequest(new { message = "Specialization and LicenseNumber are required for Doctor role" });
                }
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            // Create AppUser
            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Failed to create user",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, request.RoleName);

            // If Doctor role, create Doctor entity
            if (request.RoleName == "Doctor")
            {
                var doctor = new Doctor
                {
                    UserId = user.Id,
                    Specialization = request.Specialization!,
                    LicenseNumber = request.LicenseNumber!
                };

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = $"{request.RoleName} user created successfully",
                userId = user.Id,
                email = user.Email
            });
        }
    }
}