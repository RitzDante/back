namespace ActiveCitizen.Web.Services
{
    public interface IApiClient
    {
        Task<T> GetAsync<T>(string requestUri);

        Task<T> PostAsync<T>(string requestUri, object data);

        Task<T> PutAsync<T>(string requestUri, object data);

        Task<bool> DeleteAsync(string requestUri);

        Task<(byte[] Bytes, string ContentType, string FileName)> GetFileAsync(string requestUri);
    }
}