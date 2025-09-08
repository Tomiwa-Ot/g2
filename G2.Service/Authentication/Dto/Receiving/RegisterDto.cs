namespace G2.Service.Authentication.Dto.Receiving
{
    public class RegisterDto
    {
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? ReferralCode { get; set; }
    }
}