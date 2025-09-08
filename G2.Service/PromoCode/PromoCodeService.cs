using AutoMapper;
using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.PromoCode;
using G2.Service.Helper;
using G2.Service.PromoCode.Dto.Receiving;
using G2.Service.PromoCode.Validation;
using Microsoft.Extensions.Logging;

namespace G2.Service.PromoCode
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly AddPromoCodeValidator _addPromoCodeValidator;
        private readonly VerifyPromoCodeValidator _verifyPromoCodeValidator;
        private readonly ProfileHelper _profileHelper;
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IPromoCodeService> _logger;

        public PromoCodeService(AddPromoCodeValidator addPromoCodeValidator,
                    VerifyPromoCodeValidator verifyPromoCodeValidator,
                    ProfileHelper profileHelper,
                    IPromoCodeRepository promoCodeRepository,
                    IPlanRepository planRepository,
                    IMapper mapper,
                    IUnitOfWork unitOfWork,
                    ILogger<IPromoCodeService> logger)
        {
            _addPromoCodeValidator = addPromoCodeValidator;
            _verifyPromoCodeValidator = verifyPromoCodeValidator;
            _profileHelper = profileHelper;
            _promoCodeRepository = promoCodeRepository;
            _planRepository = planRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Response> AddPromoCode(AddPromoCodeDto addPromoCodeDto)
        {
            try
            {
                // Validate details
                ValidationResult validationResult = await _addPromoCodeValidator.ValidateAsync(addPromoCodeDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }
                
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null 
                    || !user.Role.Equals("super-admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                await _unitOfWork.BeginTransactionAsync();

                Infrastructure.Model.PromoCode newPromoCode = _mapper.Map<Infrastructure.Model.PromoCode>(addPromoCodeDto);
                newPromoCode.CreatedAt = DateTime.UtcNow.AddHours(1);
                newPromoCode.UsageCount = 0;
                newPromoCode.IsDeleted = false;

                Infrastructure.Model.PromoCode promoCode = await _promoCodeRepository.AddAsync(newPromoCode);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    promoCode.Id,
                    promoCode.Code,
                    promoCode.Discount,
                    promoCode.UsageLimit,
                    promoCode.ExpiredAt,
                });
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> DeletePromoCode(long id)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Equals("super-admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                await _unitOfWork.BeginTransactionAsync();
                Infrastructure.Model.PromoCode? promoCode = await _promoCodeRepository.FirstOrDefaultAsync(x
                    => !x.IsDeleted);
                if (promoCode == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Promo code doesn't exist.", null);
                }

                promoCode.IsDeleted = true;
                _promoCodeRepository.Delete(promoCode);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Success", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetAllPromoCodes(int page = 1, int limit = 10)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                page = Math.Max(page, 1);
                limit = Math.Clamp(limit, 1, 100);
                int skip = (page - 1) * limit;

                List<Infrastructure.Model.PromoCode> promoCodes = await _promoCodeRepository.FindListAsync(x
                    => !x.IsDeleted,
                    orderBy: q => q.OrderByDescending(e => e.Id),
                    skip: skip,
                    take: limit);
                
                return ResponseBuilder.Send(ResponseStatus.success, "Success", promoCodes);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetPromoCode(long id)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Infrastructure.Model.PromoCode? promoCode = await _promoCodeRepository.FirstOrDefaultAsync(x =>
                    x.Id == id && !x.IsDeleted);

                return ResponseBuilder.Send(ResponseStatus.success, "Success", promoCode);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> UpdatePromoCode(long id, UpdatePromoCodeDto updatePromoCodeDto)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Equals("super-admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                await _unitOfWork.BeginTransactionAsync();

                Infrastructure.Model.PromoCode? promoCode = await _promoCodeRepository.FirstOrDefaultAsync(
                    x => !x.IsDeleted);
                if (promoCode == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Promo code doesn't exist", null);
                }

                promoCode.Code = updatePromoCodeDto.Code ?? promoCode.Code;
                promoCode.Discount = updatePromoCodeDto.Discount ?? promoCode.Discount;
                promoCode.UsageLimit = updatePromoCodeDto.UsageLimit ?? promoCode.UsageLimit;
                promoCode.ExpiredAt = updatePromoCodeDto.ExpiredAt ?? promoCode.ExpiredAt;

                _promoCodeRepository.Update(promoCode);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Success", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> VerifyPromoCode(VerifyPromoCodeDto verifyPromoCodeDto)
        {
            try
            {
                // Validate details
                ValidationResult validationResult = await _verifyPromoCodeValidator.ValidateAsync(verifyPromoCodeDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                //  Check if promo code exists
                Infrastructure.Model.PromoCode? promoCode = await _promoCodeRepository.FirstOrDefaultAsync(x
                    => !x.IsDeleted && DateTime.Today < x.ExpiredAt && x.UsageCount < x.UsageLimit
                        && x.Code.Equals(verifyPromoCodeDto.Code, StringComparison.OrdinalIgnoreCase));
                if (promoCode == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Check if plan exists
                Infrastructure.Model.Plan? plan = await _planRepository.FirstOrDefaultAsync(x
                    => !x.IsDeleted && x.Id == verifyPromoCodeDto.PlanId);
                if (plan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                double originalPrice = plan.Price - (plan.Price * (plan.Discount / 100));
                originalPrice = verifyPromoCodeDto.IsYearly ? originalPrice * 12 : originalPrice;
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    PlanId = plan.Id,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = originalPrice - (originalPrice * (promoCode.Discount / 100))
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}