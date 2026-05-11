using ActiveCitizen.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActiveCitizen.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _authService.AuthenticateAsync(model.Email, model.Password);
        if (user == null) 
            return Unauthorized(new {message = "Неверная почта или пароль"});
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
                DistrictId = user.DistrictId,
                Role = user.Role?.Name
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        try
        {
            var user = await _authService.RegisterAsync(
                model.Email,
                model.Password,
                model.FullName,
                model.PhoneNumber);

            return Ok(new { message = "Регистрация успешна", userId = user.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new {message = ex.Message});
        }
    }

    [HttpPost("register-inspector")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterInspector([FromBody] RegisterInspectorModel model)
    {
        try
        {
            var user = await _authService.RegisterInspectorAsync(
                model.Email,
                model.Password,
                model.FullName,
                model.DistrictId);

            return Ok(new { message = "Инспектор успешно создан", userId = user.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
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
        public string FullName {  get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class RegisterInspectorModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public int DistrictId { get; set; }
    }
}
