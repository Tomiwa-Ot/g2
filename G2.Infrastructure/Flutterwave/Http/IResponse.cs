namespace G2.Infrastructure.Flutterwave.Http
{
    public interface IResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<Error>? Errors { get; set; }
    }

    public class Error
    {
        public string? Field { get; set; }
        public string? Message { get; set; }
    }
}