namespace G2.Infrastructure.Model
{
    public class Response
    {
        public string Status { get; set; }
        public string? Message { get; set; }
        public object? Body { get; set; }
    }

    public enum ResponseStatus
    {
        success,
        failure,
        invalid_format,
        unauthorised,
        quota_exceeded,
        max_concurrent_jobs,
        invalid_promo_code,
        expired_promo_code,
        payment_otp_required,
        redirect_for_payment
    }
}