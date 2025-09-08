using G2.Infrastructure.Model;

namespace G2.Service.Stats
{
    public interface IStatsService
    {
        Task<Response> Payment(DateTime? from, DateTime? to);
        Task<Response> Health();
        Task<Response> Users(DateTime? from, DateTime? to);
        Task<Response> Jobs(DateTime? from, DateTime? to);
    }
}