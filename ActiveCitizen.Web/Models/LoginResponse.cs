using System.Text.Json.Serialization;

namespace ActiveCitizen.Web.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("user")]
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("districtId")]
        public int? DistrictId { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }
}
