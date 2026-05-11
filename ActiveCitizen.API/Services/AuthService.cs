using ActiveCitizen.API.Data;
using ActiveCitizen.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Success ? user : null;
        }

        public async Task<User> RegisterAsync(string email, string password, string fullName, string? phoneNumber)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                throw new Exception("Пользователь с такой почтой уже существует");
            var citizenRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Citizen");
            if (citizenRole == null)
                throw new Exception("Роль 'Гражданин' не найдена в БД");

            var user = new User
            {
                Email = email,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                RegistrationDate = DateTime.UtcNow,
                RoleId = citizenRole.Id
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role?.Name ?? "Citizen"),
                new System.Security.Claims.Claim("fullName", user.FullName)
            };

            if (user.DistrictId.HasValue)
            {
                claims.Add(new System.Security.Claims.Claim("DistrictId", user.DistrictId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> RegisterInspectorAsync(string email, string password, string fullName, int districtId)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                throw new Exception("Пользователь с такой почтой уже существует");

            var inspectorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Inspector");
            if (inspectorRole == null)
                throw new Exception("Роль 'Инспектор' не найден в БД");

            var district = await _context.Districts.FindAsync(districtId);
            if (district == null)
                throw new Exception("Указанный район не существует");

            var user = new User
            {
                Email = email,
                FullName = fullName,
                RegistrationDate = DateTime.UtcNow,
                RoleId = inspectorRole.Id,
                DistrictId = districtId
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
