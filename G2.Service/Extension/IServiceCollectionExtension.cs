using ErcasPay.Extensions;
using FluentValidation;
using G2.Infrastructure;
using G2.Service.Authentication;
using G2.Service.Authentication.Validation;
using G2.Service.Helper;
using G2.Service.Jobs;
using G2.Service.Messages;
using G2.Service.Jobs.Validation;
using G2.Service.Plan;
using G2.Service.Plan.Validation;
using G2.Service.Transaction;
using G2.Service.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using G2.Service.Transaction.Validation;
using G2.Service.Messages.Validation;
using G2.Service.Stats;
using G2.Service.PromoCode.Validation;
using G2.Service.PromoCode;

namespace G2.Service.Extension
{
    public static class IServiceCollectionsExtension
    {
        public static IServiceCollection AddG2Services(this IServiceCollection services, IConfiguration config)
        {
            services.AddHostedService<ConsumerService>();
            services.AddHostedService<UserBackgroundService>();
            services.AddHostedService<TransactionBackgroundService>();
            services.AddSingleton<EmailHelper>();
            services.AddSingleton<JwtHelper>();
            services.AddSingleton<ProfileHelper>();
            services.AddSingleton<SystemLoad>();
            services.AddInfrastructure(config);
            services.AddValidatorsFromAssemblyContaining<AddPromoCodeValidator>();
            services.AddValidatorsFromAssemblyContaining<AddJobValidator>();
            services.AddValidatorsFromAssemblyContaining<AddMessageValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateMessageValidator>();
            services.AddValidatorsFromAssemblyContaining<AddTransactionValidator>();
            services.AddValidatorsFromAssemblyContaining<GithubValidator>();
            services.AddValidatorsFromAssemblyContaining<GoogleValidator>();
            services.AddValidatorsFromAssemblyContaining<ForgotPasswordValidator>();
            services.AddValidatorsFromAssemblyContaining<LoginValidator>();
            services.AddValidatorsFromAssemblyContaining<PasswordResetValidator>();
            services.AddValidatorsFromAssemblyContaining<RegistrationValidator>();
            services.AddValidatorsFromAssemblyContaining<TokenRenewalValidator>();
            services.AddValidatorsFromAssemblyContaining<User.Validation.PasswordResetValidator>();
            services.AddValidatorsFromAssemblyContaining<AddPlanValidator>();
            services.AddValidatorsFromAssemblyContaining<VerifyPromoCodeValidator>();

            services.AddErcasPay(config);

            return services
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<IConsumerService, ConsumerService>()
                .AddScoped<IUserBackgroundService, UserBackgroundService>()
                .AddScoped<ITransactionBackgroundService, TransactionBackgroundService>()
                .AddScoped<IJobService, JobService>()
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IPlanService, PlanService>()
                .AddScoped<IPromoCodeService, PromoCodeService>()
                .AddScoped<IStatsService, StatsService>()
                .AddScoped<ITransactionService, TransactionService>()
                .AddScoped<IUserService, UserService>();
        }
    }
}
