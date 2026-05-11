using ActiveCitizen.API.Data;
using ActiveCitizen.API.DTOs;
using ActiveCitizen.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActiveCitizen.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DistrictsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DistrictsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts()
        {
            var disctricts = await _context.Districts
                .Select(d => new DistrictDto
                {
                Id = d.Id,
                Name = d.Name
            })
                .ToListAsync();
            return Ok(disctricts);
        }
    }
}
