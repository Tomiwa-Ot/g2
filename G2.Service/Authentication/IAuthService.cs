using G2.Infrastructure.Model;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication
{
    public interface IAuthService
    {
        Task<Response> Login(LoginDto loginDto, bool mobile);
        Task<Response> Register(RegisterDto registerDto);
        Task<Response> RenewToken(RenewTokenDto renewTokenDto);
        Task<Response> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<Response> ForgotPassword(ForgotPasswordDto forgotPasswordDto);
        Task<Response> VerifyAccount(VerifyAccountDto verifyAccountDto);
        Task<Response> ResendCode(ResendCodeDto resendCodeDto);
        Task<Response> SignInWithGithub(GithubDto githubDto);
        Task<Response> SignInWithGoogle(GoogleDto googleDto);
    }
}