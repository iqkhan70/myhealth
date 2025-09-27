using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    public class UserUpdateRequest
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
    }

    namespace SM_MentalHealthApp.Server.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class PatientController : ControllerBase
        {
            private readonly UserService _userService;

            public PatientController(UserService userService)
            {
                _userService = userService;
            }

            [HttpPost]
            public async Task<ActionResult<User>> CreateUser([FromBody] User user)
            {
                try
                {
                    var createdUser = await _userService.CreateUserAsync(user);
                    return Ok(createdUser);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<User>> GetUser(int id)
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }

            [HttpGet("email/{email}")]
            public async Task<ActionResult<User>> GetUserByEmail(string email)
            {
                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }

            [HttpGet]
            public async Task<ActionResult<List<User>>> GetAllUsers()
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }

            [HttpPut("{id}")]
            public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UserUpdateRequest request)
            {
                if (id != request.Id)
                {
                    return BadRequest("ID mismatch");
                }

                try
                {
                    var user = new User
                    {
                        Id = request.Id,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        DateOfBirth = request.DateOfBirth,
                        Gender = request.Gender,
                        RoleId = request.RoleId,
                        IsActive = request.IsActive,
                        Specialization = request.Specialization,
                        LicenseNumber = request.LicenseNumber
                    };

                    var updatedUser = await _userService.UpdateUserAsync(user);
                    return Ok(updatedUser);
                }
                catch (InvalidOperationException ex)
                {
                    return NotFound(ex.Message);
                }
            }

            [HttpDelete("{id}")]
            public async Task<ActionResult> DeactivateUser(int id)
            {
                var result = await _userService.DeactivateUserAsync(id);
                if (!result)
                {
                    return NotFound();
                }
                return NoContent();
            }

            [HttpGet("demo")]
            public async Task<ActionResult<User>> GetDemoUser()
            {
                var demoUser = await _userService.GetOrCreateDemoUserAsync();
                return Ok(demoUser);
            }

            [HttpGet("{id}/stats")]
            public async Task<ActionResult<UserStats>> GetUserStats(int id)
            {
                try
                {
                    var stats = await _userService.GetUserStatsAsync(id);
                    return Ok(stats);
                }
                catch (InvalidOperationException ex)
                {
                    return NotFound(ex.Message);
                }
            }
        }
    }
}
