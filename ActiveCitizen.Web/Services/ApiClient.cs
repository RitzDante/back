using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ActiveCitizen.Web.Services
{
    public class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiClient(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
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
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private Task<string?> GetTokenAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = user.FindFirst("AccessToken");
                return Task.FromResult(tokenClaim?.Value);
            }

            return Task.FromResult<string?>(null);
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            var client = await CreateClientWithTokenAsync();

            var response = await client.GetAsync(requestUri);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(json);

            return JsonSerializer.Deserialize<T>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        }

        public async Task<T> PostAsync<T>(string requestUri, object data)
        {
            var client = await CreateClientWithTokenAsync();

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUri, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(responseJson);

            if (string.IsNullOrWhiteSpace(responseJson))
                return default!;

            return JsonSerializer.Deserialize<T>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        }

        public async Task<T> PutAsync<T>(string requestUri, object data)
        {
            var client = await CreateClientWithTokenAsync();

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(requestUri, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(responseJson);

            if (string.IsNullOrWhiteSpace(responseJson))
                return default!;

            return JsonSerializer.Deserialize<T>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        }

        public async Task<bool> DeleteAsync(string requestUri)
        {
            var client = await CreateClientWithTokenAsync();

            var response = await client.DeleteAsync(requestUri);

            return response.IsSuccessStatusCode;
        }

        public async Task<(byte[] Bytes, string ContentType, string FileName)> GetFileAsync(string requestUri)
        {
            var client = await CreateClientWithTokenAsync();

            var response = await client.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(error);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            var fileName =
                response.Content.Headers.ContentDisposition?.FileNameStar ??
                response.Content.Headers.ContentDisposition?.FileName ??
                "photo.jpg";

            fileName = fileName.Trim('"');

            return (bytes, contentType, fileName);
        }
    }
}