using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ActiveCitizen.API.Services
{
    public interface IYandexGeocoderService
    {
        Task<string?> GetAddressAsync(decimal latitude, decimal longitude);
    }

    public class YandexGeocoderService : IYandexGeocoderService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public YandexGeocoderService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Ключ берём из appsettings.json или используйте ваш, если ещё не вынесли
            _apiKey = configuration["Yandex:ApiKey"] ?? "42e402f2-14ec-4e1f-b096-d2cb222a0d9a";
        }

        public async Task<string?> GetAddressAsync(decimal latitude, decimal longitude)
        {
            // Формат запроса к геокодеру Яндекса (обратное геокодирование)
            var url = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}&format=json&geocode={longitude},{latitude}&lang=ru_RU";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(content);

            // Извлекаем первый найденный адрес
            var geoObjects = json.RootElement
                .GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember");

            if (geoObjects.GetArrayLength() == 0)
                return null;

            var first = geoObjects[0]
                .GetProperty("GeoObject")
                .GetProperty("metaDataProperty")
                .GetProperty("GeocoderMetaData")
                .GetProperty("text");

            return first.GetString();
        }
    }
}