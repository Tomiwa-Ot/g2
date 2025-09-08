using G2.Infrastructure.Flutterwave.Checkout;
using G2.Infrastructure.Flutterwave.Http;
using G2.Infrastructure.Repository;
using G2.Infrastructure.Repository.Database.AccountVerification;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.KnownHeader;
using G2.Infrastructure.Repository.Database.Job;
using G2.Infrastructure.Repository.Database.Message;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.PromoCode;
using G2.Infrastructure.Repository.Database.Referral;
using G2.Infrastructure.Repository.Database.Role;
using G2.Infrastructure.Repository.Database.Transaction;
using G2.Infrastructure.Repository.Database.User;
using G2.Infrastructure.Repository.MessageQueue;
using G2.Infrastructure.TechnologyDetector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace G2.Infrastructure
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<Func<G2DbContext>>((provider) 
                => () => provider.GetService<G2DbContext>());
            services.AddScoped<DbFactory>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            services.AddDbContext<G2DbContext>(options =>
                    options.UseMySQL(config.GetSection("Database")["ConnectionString"]));

            services.AddScoped<IApiClient, ApiClient>();
            services.AddScoped<IRequest, Request>();

            services.AddHttpClient();

            return services
                .AddScoped(typeof(IDbRepository<>), typeof(DbRepository<>))
                .AddScoped<IAccountVerificationRepository, AccountVerificationRepository>()
                .AddSingleton<IMemoryCache, MemoryCache>()
                .AddSingleton<IMessageQueue, MessageQueue>()
                .AddScoped<IWappalyzer, Wappalyzer>()
                .AddScoped<IJobRepository, JobRepository>()
                .AddScoped<IKnownHeaderRepository, KnownHeaderRepository>()
                .AddScoped<IMessageRepository, MessageRepository>()
                .AddScoped<IPlanRepository, PlanRepository>()
                .AddScoped<IPromoCodeRepository, PromoCodeRepository>()
                .AddScoped<IReferralRepository, ReferralRepository>()
                .AddScoped<IRoleRepository, RoleRepository>()
                .AddScoped<ITransactionRepository, TransactionRepository>()
                .AddScoped<IUserRepository, UserRepository>();
        }
    }
}