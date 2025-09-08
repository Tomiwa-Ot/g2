using AutoMapper;
using G2.Infrastructure.Model;
using G2.Service.Authentication.Dto.Receiving;
using G2.Service.Jobs.Dto.Receiving;
using G2.Service.Jobs.Dto.Transfer;
using G2.Service.Messages.Dto.Receiving;
using G2.Service.Plan.Dto.Transfer;
using G2.Service.PromoCode.Dto.Receiving;
using G2.Service.Transaction.Dto.Transfer;

namespace G2.API.Helper
{
    public class AutoMapper: Profile
    {
        public AutoMapper()
        {
            CreateMap<AddJobDto, Job>().ReverseMap();
            CreateMap<AddMessageDto, Message>().ReverseMap();
            CreateMap<JobDto, Job>().ReverseMap();
            CreateMap<RegisterDto, User>().ReverseMap();
            CreateMap<TransactionDto, Transaction>().ReverseMap();
            CreateMap<PlanDto, Plan>().ReverseMap();
            CreateMap<AddPromoCodeDto, PromoCode>().ReverseMap();
        }
    }
}