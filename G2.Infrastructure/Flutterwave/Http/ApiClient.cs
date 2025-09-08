using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace G2.Infrastructure.Flutterwave.Http
{
    public class ApiClient : IApiClient
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiClient(IConfiguration configuration,
             IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<T> Send<T>(HttpMethod method, string endpoint, object? body = null) where T : IResponse
        {   
            HttpClient client = _httpClientFactory.CreateClient();
            HttpRequestMessage request = await BuildRequest(method, endpoint, body);     
            
            // Send http request
            HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            
            // Deserialize json response into specified model
            T result = JsonConvert.DeserializeObject<T>(responseBody);
            return result;
        }

        private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string endpoint, object? body)
        {
            // Load flutterwave base url from appsettings
            string baseUrl = _configuration.GetSection("Flutterwave:BaseUrl").Value.EndsWith("/") ?
                $"{_configuration.GetSection("Flutterwave:BaseUrl").Value}{endpoint}" :
                $"{_configuration.GetSection("Flutterwave:BaseUrl").Value}/{endpoint}";

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new Exception("Base URL not found in configuration");
            }

            // Load authorization token from appsettings
            string authorizationKey = _configuration.GetSection("Flutterwave:SecretKey").Value;
            if (string.IsNullOrEmpty(authorizationKey))
            {
                throw new Exception("Authorization key not found in configuration");
            }

            // Build HTTP request header and body
            HttpRequestMessage request = new HttpRequestMessage(method, new Uri(baseUrl));
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {authorizationKey}");

            if (body != null)
            {
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8, 
                    "application/json"
                );
            }

            return request;
        }
    }
}
