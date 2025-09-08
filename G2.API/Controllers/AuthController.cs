using G2.Infrastructure.Model;
using G2.Service.Authentication;
using G2.Service.Authentication.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        public async Task<Response> Login([FromBody] LoginDto loginDto, [FromQuery] bool? mobile = false)
        {
            return await _authService.Login(loginDto, mobile.Value);
        }

        [HttpPost("register")]
        public async Task<Response> Register(RegisterDto registerDto)
        {
            return await _authService.Register(registerDto);
        }

        [HttpPost("forgot-password")]
        public async Task<Response> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            return await _authService.ForgotPassword(forgotPasswordDto);
        }

        [HttpPost("reset-password")]
        public async Task<Response> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            return await _authService.ResetPassword(resetPasswordDto);
        }

        [HttpPost("renew-token")]
        public async Task<Response> RenewToken(RenewTokenDto renewTokenDto)
        {
            return await _authService.RenewToken(renewTokenDto);
        }

        [HttpPost("verify")]
        public async Task<Response> VerifyAccount(VerifyAccountDto verifyAccountDto)
        {
            return await _authService.VerifyAccount(verifyAccountDto);
        }

        [HttpPost("resend-code")]
        public async Task<Response> ResendCode(ResendCodeDto resendCodeDto)
        {
            return await _authService.ResendCode(resendCodeDto);
        }

        [HttpPost("github")]
        public async Task<Response> SignInWithGithub(GithubDto githubDto)
        {
            return await _authService.SignInWithGithub(githubDto);
        }
        
        [HttpPost("google")]
        public async Task<Response> SignInWithGoogle(GoogleDto googleDto)
        {
            return await _authService.SignInWithGoogle(googleDto);
        }
    }
}