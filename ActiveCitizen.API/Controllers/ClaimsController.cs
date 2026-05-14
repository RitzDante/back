using ActiveCitizen.API.Data;
using ActiveCitizen.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ActiveCitizen.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimsController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ClaimsController(
            ApplicationDbContext context,
            ILogger<ClaimsController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        // GET: api/claims/my
        [HttpGet("my")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetMyClaims()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Не удалось определить пользователя." });

            var claims = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status != null ? c.Status.Name : string.Empty,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType != null ? c.ViolationType.Name : string.Empty,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    PhotoPath = c.PhotoPath
                })
                .ToListAsync();

            return Ok(claims);
        }

        // POST: api/claims
        [HttpPost]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> CreateClaim([FromForm] CreateClaimDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Не удалось определить пользователя." });

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден." });

            var violationType = await _context.ViolationTypes
                .FirstOrDefaultAsync(v => v.Id == model.ViolationTypeId);

            if (violationType == null)
                return BadRequest(new { message = "Указанный тип нарушения не найден." });

            var districtName = NormalizeText(model.DistrictName);

            if (string.IsNullOrWhiteSpace(districtName))
                return BadRequest(new { message = "Район не передан." });

            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == districtName.ToLower());

            if (district == null)
            {
                return BadRequest(new
                {
                    message = $"Район '{model.DistrictName}' не найден в справочнике Districts."
                });
            }

            var status = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Id == 1);

            if (status == null)
                return BadRequest(new { message = "Статус с Id = 1 не найден в справочнике Statuses." });

            var address = NormalizeText(model.Address);

            if (string.IsNullOrWhiteSpace(address))
                return BadRequest(new { message = "Адрес не передан." });

            var claim = new ActiveCitizen.API.Models.Claim
            {
                UserId = userId.Value,
                StatusId = 1,
                ViolationTypeId = violationType.Id,
                DistrictId = district.Id,
                Address = address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                PhotoPath = null,
                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            if (model.Photo != null && model.Photo.Length > 0)
            {
                claim.PhotoPath = await SaveClaimPhotoAsync(
                    model.Photo,
                    claim.Id,
                    district.Id,
                    district.Name
                );

                await _context.SaveChangesAsync();
            }

            var createdClaimDto = new ClaimDto
            {
                Id = claim.Id,
                Description = claim.Description,
                Address = claim.Address,
                Latitude = claim.Latitude,
                Longitude = claim.Longitude,
                StatusName = status.Name,
                StatusId = claim.StatusId,
                ViolationTypeName = violationType.Name,
                ViolationTypeId = claim.ViolationTypeId,
                CreatedAt = claim.CreatedAt ?? DateTime.MinValue,
                PhotoPath = claim.PhotoPath
            };

            return CreatedAtAction(nameof(GetMyClaims), new { id = createdClaimDto.Id }, createdClaimDto);
        }

        // GET: api/claims/violation-types
        [HttpGet("violation-types")]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> GetViolationTypes()
        {
            var types = await _context.ViolationTypes
                .OrderBy(v => v.Id)
                .Select(v => new
                {
                    v.Id,
                    v.Name
                })
                .ToListAsync();

            return Ok(types);
        }

        // GET: api/claims/statuses
        [HttpGet("statuses")]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> GetStatuses()
        {
            var statuses = await _context.Statuses
                .OrderBy(s => s.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Name
                })
                .ToListAsync();

            return Ok(statuses);
        }

        // GET: api/claims
        [HttpGet]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> GetClaimsForInspector()
        {
            var districtId = GetCurrentInspectorDistrictId();

            if (districtId == null)
                return Forbid();

            var claims = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.DistrictId == districtId.Value)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status != null ? c.Status.Name : string.Empty,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType != null ? c.ViolationType.Name : string.Empty,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    PhotoPath = c.PhotoPath
                })
                .ToListAsync();

            return Ok(claims);
        }

        // GET: api/claims/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> GetClaimByIdForInspector(int id)
        {
            var districtId = GetCurrentInspectorDistrictId();

            if (districtId == null)
                return Forbid();

            var claim = await _context.Claims
                .Include(c => c.Status)
                .Include(c => c.ViolationType)
                .Where(c => c.Id == id && c.DistrictId == districtId.Value)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    StatusName = c.Status != null ? c.Status.Name : string.Empty,
                    StatusId = c.StatusId,
                    ViolationTypeName = c.ViolationType != null ? c.ViolationType.Name : string.Empty,
                    ViolationTypeId = c.ViolationTypeId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    PhotoPath = c.PhotoPath
                })
                .FirstOrDefaultAsync();

            if (claim == null)
            {
                return NotFound(new
                {
                    message = "Заявка не найдена или недоступна для текущего инспектора."
                });
            }

            return Ok(claim);
        }

        // GET: api/claims/{id}/photo
        [HttpGet("{id:int}/photo")]
        [Authorize(Roles = "Citizen,Inspector")]
        public async Task<IActionResult> GetClaimPhoto(int id)
        {
            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
                return NotFound(new { message = "Заявка не найдена." });

            if (!CanCurrentUserAccessClaim(claim))
                return Forbid();

            if (string.IsNullOrWhiteSpace(claim.PhotoPath))
                return NotFound(new { message = "Фото у заявки отсутствует." });

            var photoFullPath = GetPhotoFullPath(claim.PhotoPath);

            if (!System.IO.File.Exists(photoFullPath))
                return NotFound(new { message = "Файл фотографии не найден на сервере." });

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(photoFullPath, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(photoFullPath, contentType, Path.GetFileName(photoFullPath));
        }

        // PUT: api/claims/{id}/status
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Inspector")]
        public async Task<IActionResult> UpdateClaimStatus(int id, [FromBody] UpdateStatusDto updateStatusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var districtId = GetCurrentInspectorDistrictId();

            if (districtId == null)
                return Forbid();

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == id && c.DistrictId == districtId.Value);

            if (claim == null)
            {
                return NotFound(new
                {
                    message = "Заявка не найдена или недоступна для текущего инспектора."
                });
            }

            var status = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Id == updateStatusDto.StatusId);

            if (status == null)
            {
                return BadRequest(new
                {
                    message = "Указанный статус не существует."
                });
            }

            claim.StatusId = updateStatusDto.StatusId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса заявки {ClaimId}", id);

                return StatusCode(500, new
                {
                    message = "Произошла ошибка при сохранении изменений."
                });
            }

            return Ok(new
            {
                message = "Статус заявки обновлён.",
                claimId = claim.Id,
                statusId = claim.StatusId,
                statusName = status.Name
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdString =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdString))
                return null;

            if (!int.TryParse(userIdString, out var userId))
                return null;

            if (userId <= 0)
                return null;

            return userId;
        }

        private int? GetCurrentInspectorDistrictId()
        {
            var districtIdString = User.FindFirst("DistrictId")?.Value;

            if (string.IsNullOrWhiteSpace(districtIdString))
            {
                _logger.LogWarning(
                    "DistrictId claim отсутствует у пользователя {User}",
                    User.Identity?.Name
                );

                return null;
            }

            if (!int.TryParse(districtIdString, out var districtId))
            {
                _logger.LogError(
                    "DistrictId claim имеет неверный формат: {DistrictIdClaim}",
                    districtIdString
                );

                return null;
            }

            if (districtId <= 0)
                return null;

            return districtId;
        }

        private bool CanCurrentUserAccessClaim(ActiveCitizen.API.Models.Claim claim)
        {
            if (User.IsInRole("Inspector"))
            {
                var districtId = GetCurrentInspectorDistrictId();
                return districtId != null && claim.DistrictId == districtId.Value;
            }

            if (User.IsInRole("Citizen"))
            {
                var userId = GetCurrentUserId();
                return userId != null && claim.UserId == userId.Value;
            }

            return false;
        }

        private string GetPhotoFullPath(string photoPath)
        {
            if (Path.IsPathRooted(photoPath))
                return photoPath;

            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRootPath, photoPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private async Task<string> SaveClaimPhotoAsync(
            IFormFile photo,
            int claimId,
            int districtId,
            string districtName)
        {
            var rootPath = _configuration["ClaimPhotosRoot"];

            if (string.IsNullOrWhiteSpace(rootPath))
                rootPath = @"D:\gati-claims-foto";

            var districtFolderName = GetDistrictFolderName(districtId, districtName);
            var districtFolderPath = Path.Combine(rootPath, districtFolderName);

            Directory.CreateDirectory(districtFolderPath);

            var extension = Path.GetExtension(photo.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var fileName = $"claim_{claimId}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            var fullFilePath = Path.Combine(districtFolderPath, fileName);

            await using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            return fullFilePath;
        }

        private static string GetDistrictFolderName(int districtId, string districtName)
        {
            return districtId switch
            {
                1 => "1 Admiralteyski",
                2 => "2 Vasileostrovski",
                3 => "3 Vyborgski",
                4 => "4 Kalininski",
                5 => "5 Kirovski",
                6 => "6 Kolpinski",
                7 => "7 Krasnogvardeyski",
                8 => "8 Krasnoselski",
                9 => "9 Kronshtadtski",
                10 => "10 Kurortni",
                11 => "11 Moskovski",
                12 => "12 Nevski",
                13 => "13 Petrogradski",
                14 => "14 Petrodvortsovi",
                15 => "15 Primorski",
                16 => "16 Pushkinski",
                17 => "17 Frunzenski",
                18 => "18 Tsentralni",
                _ => $"{districtId} {MakeSafeFolderName(districtName)}"
            };
        }

        private static string MakeSafeFolderName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Trim();
        }
    }
}