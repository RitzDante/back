using ActiveCitizen.API.Models;

namespace ActiveCitizen.API.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> RegisterAsync(string email, string password, string fullName, string? phoneNumber);
        Task<User> RegisterInspectorAsync(string  email, string password, string fullName, int districtId);
        string GenerateJwtToken(User user);

    }
}
