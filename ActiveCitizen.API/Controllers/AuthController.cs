using ActiveCitizen.API.Data;
using ActiveCitizen.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActiveCitizen.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        // POST: api/auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Введите почту и пароль" });
            }

            var user = await _authService.AuthenticateAsync(model.Email, model.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Неверная почта или пароль" });
            }

            var roleName = await _context.Roles
                .Where(r => r.Id == user.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();

            var token = _authService.GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.DistrictId,
                    user.NotificationsEnabled,
                    Role = roleName
                }
            });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { message = "Введите почту" });
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Введите пароль" });
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return BadRequest(new { message = "Введите ФИО" });
            }

            try
            {
                var user = await _authService.RegisterAsync(
                    model.Email,
                    model.Password,
                    model.FullName,
                    model.PhoneNumber
                );

                return Ok(new
                {
                    message = "Регистрация успешна",
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/auth/register-inspector
        [HttpPost("register-inspector")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterInspector([FromBody] RegisterInspectorModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { message = "Введите почту инспектора" });
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Введите пароль инспектора" });
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return BadRequest(new { message = "Введите ФИО инспектора" });
            }

            if (model.DistrictId <= 0)
            {
                return BadRequest(new { message = "Выберите район инспектора" });
            }

            try
            {
                var user = await _authService.RegisterInspectorAsync(
                    model.Email,
                    model.Password,
                    model.FullName,
                    model.DistrictId
                );

                return Ok(new
                {
                    message = "Инспектор успешно создан",
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { message = "Введите почту" });
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { message = "Введите новый пароль" });
            }

            if (model.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Пароль должен содержать минимум 6 символов" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь с такой почтой не найден" });
            }

            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ActiveCitizen.API.Models.User>();
            user.PasswordHash = passwordHasher.HashPassword(user, model.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Пароль успешно изменён" });
        }
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public class RegisterModel
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
    }

    public class RegisterInspectorModel
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int DistrictId { get; set; }
    }

    public class ForgotPasswordModel
    {
        public string Email { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}