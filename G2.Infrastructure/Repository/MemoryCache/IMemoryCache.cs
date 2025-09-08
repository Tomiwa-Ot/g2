namespace G2.Infrastructure.Repository
{
    public interface IMemoryCache
    {
        Task SetString(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetString(string key);
        Task<bool> DeleteString(string key);
        Task<long> IncrementString(string key);
        Task AddToSet(string key, string value);
        Task RemoveFromSet(string key, string value);
        Task<bool> SetContains(string key, string value);
    }
}