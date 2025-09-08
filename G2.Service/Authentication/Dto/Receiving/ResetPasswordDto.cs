namespace G2.Service.Authentication.Dto.Receiving
{
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string ResetToken { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}