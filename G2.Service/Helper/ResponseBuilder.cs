using G2.Infrastructure.Model;

namespace G2.Service.Helper
{
    public class ResponseBuilder
    {
        public static Response Send(ResponseStatus status, string? message, object? body)
        {
            return new Response
            {
                Status = status.ToString(),
                Message = message,
                Body = body
            };
        }
    }
}