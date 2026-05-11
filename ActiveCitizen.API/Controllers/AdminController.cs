using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ActiveCitizen.API.Data;
using ActiveCitizen.API.Models;


namespace ActiveCitizen.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("inspectors")]
        public async Task<IActionResult> GetInspectors()
        {
            var inspector = await _context.Users
                .Where(u => u.Role.Name == "Inspector")
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    DistrictName = u.District != null ? u.District.Name : "Не назначен"
                })
                .ToListAsync();

            return Ok(inspector);
        }

        [HttpGet("inspector/{id}")]
        public async Task<IActionResult> GetInspectors(int id)
        {
            var inspector = await _context.Users
                .Where(u => u.Id == id && u.Role.Name == "Inspector")
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    DistrictId = u.DistrictId,
                    DistrictName = u.District != null ? u.District.Name : "Не назначен"
                })
                .FirstOrDefaultAsync();
            if (inspector == null)
                return NotFound();

            return Ok(inspector);
        }

        [HttpPut("inspector/{id}")]
        public async Task<IActionResult> UpdateInspector(int id, [FromBody] UpdateInspectorModel model)
        {
            var inspector = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role.Name == "Inspector");
            if(inspector == null) 
                return NotFound();
            if(model.DistrictId.HasValue)
            {
                var district = await _context.Districts.FindAsync(model.DistrictId.Value);
                if (district == null)
                    return BadRequest("Район не найден");
                inspector.DistrictId = model.DistrictId.Value;
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var hasher = new PasswordHasher<User>();
                inspector.PasswordHash = hasher.HashPassword(inspector, model.Password);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("inspector/{id}")]
        public async Task<IActionResult> DeleteInspector(int id)
        {
            var inspector = await _context.Users
                .FirstOrDefaultAsync (u => u.Id == id && u.Role.Name == "Inspector");

            if(inspector == null) 
                return NotFound();

            _context.Users.Remove(inspector);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
