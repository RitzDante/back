using ActiveCitizen.API.Data;
using ActiveCitizen.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using JwtClaim = System.Security.Claims.Claim;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace ActiveCitizen.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLower();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password
            );

            return result == PasswordVerificationResult.Success ? user : null;
        }

        public async Task<User> RegisterAsync(
            string email,
            string password,
            string fullName,
            string? phoneNumber)
        {
            var normalizedEmail = email.Trim().ToLower();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
                throw new Exception("Пользователь с такой почтой уже существует");

            var citizenRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Citizen");

            if (citizenRole == null)
                throw new Exception("Роль 'Citizen' не найдена в БД");

            var user = new User
            {
                Email = normalizedEmail,
                FullName = fullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
                    ? null
                    : phoneNumber.Trim(),
                RegistrationDate = DateTime.UtcNow,
                RoleId = citizenRole.Id
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> RegisterInspectorAsync(
            string email,
            string password,
            string fullName,
            int districtId)
        {
            var normalizedEmail = email.Trim().ToLower();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
                throw new Exception("Пользователь с такой почтой уже существует");

            var inspectorRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Inspector");

            if (inspectorRole == null)
                throw new Exception("Роль 'Inspector' не найдена в БД");

            var district = await _context.Districts.FindAsync(districtId);

            if (district == null)
                throw new Exception("Указанный район не существует");

            var user = new User
            {
                Email = normalizedEmail,
                FullName = fullName.Trim(),
                RegistrationDate = DateTime.UtcNow,
                RoleId = inspectorRole.Id,
                DistrictId = districtId
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task ResetPasswordAsync(string email, string newPassword)
        {
            var normalizedEmail = email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                throw new Exception("Пользователь с такой почтой не найден");

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            await _context.SaveChangesAsync();
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<JwtClaim>
            {
                new JwtClaim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new JwtClaim(JwtRegisteredClaimNames.Email, user.Email),
                new JwtClaim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new JwtClaim(ClaimTypes.Role, user.Role?.Name ?? "Citizen"),
                new JwtClaim("fullName", user.FullName)
            };

            if (user.DistrictId.HasValue)
            {
                claims.Add(new JwtClaim("DistrictId", user.DistrictId.Value.ToString()));
            }

            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT ключ не указан в appsettings.json");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}