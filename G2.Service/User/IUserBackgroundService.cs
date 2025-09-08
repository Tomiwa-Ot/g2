using G2.Infrastructure.Model;

namespace G2.Service.User
{
    public interface IUserBackgroundService
    {
        Task DowngradePlan();
    }
}