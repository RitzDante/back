using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace ActiveCitizen.Web.Services
{
    public class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<HttpClient> CreateClientWithTokenAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            }
            return client;
        }

        private async Task<string> GetTokenAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = user.FindFirst("AccessToken");
                return tokenClaim?.Value;
            }
            return null;
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            var client = await CreateClientWithTokenAsync();
            var response = await client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<T> PostAsync<T>(string requestUri, object data)
        {
            var client = await CreateClientWithTokenAsync();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var respose = await client.PostAsync(requestUri, content);
            respose.EnsureSuccessStatusCode();
            var responseJson = await respose.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<T> PutAsync<T>(string requestUri, object data)
        {
            var client = await CreateClientWithTokenAsync();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var respose = await client.PutAsync(requestUri, content);
            respose.EnsureSuccessStatusCode();
            var responseJson = await respose.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> DeleteAsync(string requestUri)
        {
            var client = await CreateClientWithTokenAsync();
            var response = await client.DeleteAsync(requestUri);
            return response.IsSuccessStatusCode; 
        }
    }
}
