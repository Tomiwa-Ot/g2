using Newtonsoft.Json;

namespace G2.Infrastructure.Flutterwave.Models
{
    public class Payment
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("tx_ref")]
        public string Tx_Ref { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("redirect_url")]
        public string Redirect_Url { get; set; }
        [JsonProperty("customer")]
        public Customer Customer { get; set; }
    }
}