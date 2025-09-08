namespace G2.Service.Authentication.Dto.Receiving
{
    public class RenewTokenDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}