using Newtonsoft.Json;

namespace G2.Infrastructure.Flutterwave.Models
{
    public class Customer
    {
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("phone_number")]
        public string? Phone_Number { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}