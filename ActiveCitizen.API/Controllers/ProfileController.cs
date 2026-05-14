using ActiveCitizen.API.Data;
using ActiveCitizen.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActiveCitizen.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.DistrictId,
                    u.NotificationsEnabled
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            return Ok(user);
        }

        // PUT: api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                user.FullName = model.FullName.Trim();
            }

            if (model.PhoneNumber != null)
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                    ? null
                    : model.PhoneNumber.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var normalizedEmail = model.Email.Trim();

                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == normalizedEmail && u.Id != userId);

                if (emailExists)
                {
                    return BadRequest(new { message = "Этот email уже используется" });
                }

                user.Email = normalizedEmail;
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Пароль должен содержать минимум 6 символов" });
                }

                var hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
            }

            if (model.NotificationsEnabled.HasValue)
            {
                user.NotificationsEnabled = model.NotificationsEnabled.Value;
            }

            await _context.SaveChangesAsync();

            var updatedUser = new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.DistrictId,
                user.NotificationsEnabled
            };

            return Ok(new
            {
                message = "Профиль обновлен",
                user = updatedUser
            });
        }
    }

    public class UpdateProfileModel
    {
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? NewPassword { get; set; }

        public bool? NotificationsEnabled { get; set; }
    }
}