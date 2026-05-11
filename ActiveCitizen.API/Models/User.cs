using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Security.Claims;

namespace ActiveCitizen.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public int RoleId { get; set; }
        public int? DistrictId { get; set; }

        public Role? Role { get; set; }
        public District? District { get; set; }
        public ICollection<Claim>? Claims { get; set; }

    }
}
