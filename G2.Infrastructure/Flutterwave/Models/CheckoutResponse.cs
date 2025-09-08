using G2.Infrastructure.Flutterwave.Http;

namespace G2.Infrastructure.Flutterwave.Models
{
    public class CheckoutResponse : IResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<Error>? Errors { get; set; }
        public CheckoutLink Data { get; set; }

    }

    public class CheckoutLink
    {
        public string Link { get; set; }
    }
}