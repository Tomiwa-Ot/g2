namespace G2.Infrastructure.Flutterwave.Http
{
    public interface IApiClient
    {
        Task<T> Send<T>(HttpMethod method, string endpoint, object? body = null) where T : IResponse;
    }
}