using G2.Infrastructure.Model;
using G2.Service.User.Dto.Receiving;

namespace G2.Service.User
{
    public interface IUserService
    {
        Task<Response> GetMyProfile();
        Task<Response> GetUsers(int page, int limit, string provider, string query);
        Task<Response> RegenerateAuthToken();
        Task<Response> ResetPassword(PasswordResetDto passwordResetDto);
        Task<Response> UpdateUser(UpdateUserDto updateUserDto);
        Task<Response> GetReferrals();
        Task<Response> DeactivateAccount(bool? disable);
    }
}