using G2.Infrastructure.Model;
using G2.Service.User;
using G2.Service.User.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<Response> GetMyProfile()
        {
            return await _userService.GetMyProfile();
        }

        [HttpGet("regenerate-auth-token")]
        public async Task<Response> RegenerateAuthToken()
        {
            return await _userService.RegenerateAuthToken();
        }

        [HttpPost("reset-password")]
        public async Task<Response> ResetPassword(PasswordResetDto passwordResetDto)
        {
            return await _userService.ResetPassword(passwordResetDto);
        }

        [HttpPut]
        public async Task<Response> UpdateUser(UpdateUserDto updateUserDto)
        {
            return await _userService.UpdateUser(updateUserDto);
        }

        [HttpGet("referral")]
        public async Task<Response> GetReferrals()
        {
            return await _userService.GetReferrals();
        }

        [HttpGet("deactivate")]
        public async Task<Response> DeactivateAccount([FromQuery] bool? disable = true)
        {
            return await _userService.DeactivateAccount(disable);
        }

        [HttpGet]
        [Authorize]
        public async Task<Response> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? provider = null, [FromQuery] string? query = null)
        {
            return await _userService.GetUsers(page, limit, provider, query);
        }
    }
}