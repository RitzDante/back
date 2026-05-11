using ActiveCitizen.API.Data;
using ActiveCitizen.API.DTOs;
using ActiveCitizen.API.Models;
using ActiveCitizen.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;

namespace ActiveCitizen.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimsController> _logger;
        private readonly IYandexGeocoderService _geocoder;

        public ClaimsController(ApplicationDbContext context, ILogger<ClaimsController> logger, IYandexGeocoderService geocoder) 
        {
            _context = context;
            _logger = logger;
            _geocoder = geocoder;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetMyClaims ()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var claims = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.UserId == userId)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status.Name,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType.Name,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();
            return Ok(claims);  
        }

        [HttpPost]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> CreateClaim([FromForm] CreateClaimDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Пользователь не найден");

            var violationType = await _context.ViolationTypes.FindAsync(model.ViolationTypeId);
            if (violationType == null) return BadRequest("Указанный тип нарушения не найден.");

            // --- Находим район по названию ---
            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.Name == model.DistrictName);
            if (district == null)
                return BadRequest($"Район '{model.DistrictName}' не найден в справочнике.");

            // --- Серверная унификация адреса через Яндекс ---
            if (model.Latitude != 0 && model.Longitude != 0)
            {
                var yandexAddress = await _geocoder.GetAddressAsync(model.Latitude, model.Longitude);
                if (!string.IsNullOrWhiteSpace(yandexAddress))
                    model.Address = yandexAddress;
            }

            // --- Сохранение фото ---
            string? photoPath = null;
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "claims");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{model.Photo.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(fileStream);
                }
                photoPath = Path.Combine("uploads", "claims", uniqueFileName).Replace('\\', '/');
            }

            var claim = new ActiveCitizen.API.Models.Claim
            {
                UserId = userId,
                StatusId = 1,
                ViolationTypeId = model.ViolationTypeId,
                DistrictId = district.Id,     
                Address = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                PhotoPath = photoPath,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            var createdClaimDto = new ClaimDto
            {
                Id = claim.Id,
                Description = claim.Description,
                Address = claim.Address,
                Latitude = claim.Latitude,
                Longitude = claim.Longitude,
                StatusName = claim.Status.Name,
                StatusId = claim.StatusId,
                ViolationTypeName = claim.ViolationType.Name,
                ViolationTypeId = claim.ViolationTypeId,
                CreatedAt = claim.CreatedAt ?? DateTime.MinValue
            };

            return CreatedAtAction(nameof(GetMyClaims), new { id = createdClaimDto.Id }, createdClaimDto);
        }

        [HttpGet("violation-types")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetViolationTypes()
        {
            var types = await _context.ViolationTypes
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();
            return Ok(types);
        }


        [HttpGet]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> GetClaimsForInspector()
        {
            var districtIdClaim = User.FindFirst("DistrictId")?.Value;

            if (string.IsNullOrEmpty(districtIdClaim))
            {
                _logger.LogWarning($"DistrictId claim is missing or empty for user: {User.Identity?.Name}");
                return Forbid();
            }

            if (!int.TryParse(districtIdClaim, out int districtId))
            {
                _logger.LogError($"Could not parse DistrictId claim value: '{districtIdClaim}' for user: {User.Identity?.Name}");
                return BadRequest("Неверный формат идентификатора района в токене.");
            }

            var claims = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.DistrictId == districtId)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status.Name,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType.Name,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            return Ok(claims);
        }

        // GET: api/claims/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> GetClaimByIdForInspector(int id)
        {
            var districtIdClaim = User.FindFirst("DistrictId")?.Value;

            if (string.IsNullOrEmpty(districtIdClaim))
            {
                _logger.LogWarning("DistrictId claim is missing or empty for user: {User.Identity?.Name");
                return Forbid();
            }

            if (!int.TryParse(districtIdClaim, out int districtId))
            {
                _logger.LogError($"Could not parse DistrictId claim value: '{districtIdClaim}' for user: {User.Identity?.Name}");
                return BadRequest("Неверный формат идентификатора района в токене");
            }

            var claim = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.Id == id && c.DistrictId == districtId)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status.Name,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType.Name,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue
                })
                .FirstOrDefaultAsync();

            if (claim == null)
                return NotFound("Заявка не найдена или недоступна для текущего пользователя");
            return Ok(claim);
        }

        //PUT: api/claims/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> UpdateClaimStatus(int id, [FromBody] UpdateStatusDto updateStatusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var districtIdClaim = User.FindFirst("DistrictId")?.Value;

            if (string.IsNullOrEmpty(districtIdClaim))
            {
                _logger.LogWarning("DistrictId claim is missing or empty for user: {User.Identity?.Name");
                return Forbid();
            }

            if (!int.TryParse(districtIdClaim, out int districtId))
            {
                _logger.LogError($"Could not parse DistrictId claim value: '{districtIdClaim}' for user: {User.Identity?.Name}");
                return BadRequest("Неверный формат идентификатора района в токене");
            }

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == id && c.DistrictId == districtId);
            if (claim == null) 
                return NotFound("Заявка не найдена или недоступна для текущего пользователя");

            var statusExists = await _context.Statuses.AnyAsync(s => s.Id == updateStatusDto.StatusId);

            if (!statusExists)
                return BadRequest("Указанный статус не существует.");

            claim.StatusId = updateStatusDto.StatusId;

            try
            {
                 await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса заявки {ClaimId} для пользователя {User}", id, User.Identity?.Name);
                return StatusCode(500, "Произошла ошибка при сохранении изменений");
            } 
            return NoContent();
        }
    }
}
