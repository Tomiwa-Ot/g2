using AutoMapper;
using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.User;
using G2.Service.Helper;
using G2.Service.Plan.Dto.Receiving;
using G2.Service.Plan.Dto.Transfer;
using G2.Service.Plan.Validation;
using Microsoft.Extensions.Logging;

namespace G2.Service.Plan
{
    public class PlanService : IPlanService
    {
        private readonly AddPlanValidator _addPlanValidator;
        private readonly ProfileHelper _profileHelper;
        private readonly IPlanRepository _planRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PlanService> _logger;

        public PlanService(AddPlanValidator addPlanValidator,
                        ProfileHelper profileHelper,
                        IPlanRepository planRepository,
                        IUserRepository userRepository,
                        IUnitOfWork unitOfWork,
                        IMapper mapper,
                        ILogger<PlanService> logger)
        {
            _addPlanValidator = addPlanValidator;
            _profileHelper = profileHelper;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Response> AddPlan(AddPlanDto addPlanDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _addPlanValidator.ValidateAsync(addPlanDto);
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

                Infrastructure.Model.Plan newPlan = _mapper.Map<Infrastructure.Model.Plan>(addPlanDto);
                newPlan.CreatedAt = DateTime.UtcNow.AddHours(1);
                newPlan.UpdatedAt = DateTime.UtcNow.AddHours(1);
                newPlan.IsDeleted = false;

                Infrastructure.Model.Plan createdPlan = await _planRepository.AddAsync(newPlan);
                await _unitOfWork.CommitTransactionAsync();

                PlanDto planDto = _mapper.Map<PlanDto>(createdPlan);
                planDto.OriginalPrice = createdPlan.Price;
                planDto.DiscountedPrice = createdPlan.Price - (createdPlan.Price * (createdPlan.Discount / 100));

                return ResponseBuilder.Send(ResponseStatus.success, "Plan created", planDto);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetAllPlans()
        {
            try
            {
                List<Infrastructure.Model.Plan> plans = await _planRepository.FindListAsync(x => !x.IsDeleted);
                //List<PlanDto> planDto = _mapper.Map<List<PlanDto>>(plans);

                return ResponseBuilder.Send(ResponseStatus.success, "Success", plans.Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    x.Quota,
                    x.Concurrency,
                    x.Price,
                    DiscountedPrice = x.Price - (x.Price * (x.Discount / 100)),
                    x.Discount,
                    x.AIReport,
                    x.Screenshot,
                    x.Visualisation,
                    x.ConsoleApp
                }).ToList());
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetPlan(long id)
        {
            try
            {
                Infrastructure.Model.Plan? plan = await _planRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (plan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Plan does not exist", null);
                }

                PlanDto planDto = _mapper.Map<PlanDto>(plan);
                planDto.OriginalPrice = plan.Price;
                planDto.DiscountedPrice = plan.Price - (plan.Price * (plan.Discount / 100));
                return ResponseBuilder.Send(ResponseStatus.success, "Success", planDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> DeletePlan(long id)
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

                // Search for plan
                Infrastructure.Model.Plan? plan = await _planRepository.FirstOrDefaultAsync(x => x.Id == id);
                if (plan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Plan does not exist", null);
                }

                // Delete plan
                await _unitOfWork.BeginTransactionAsync();

                plan.IsDeleted = true;
                plan.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _planRepository.Delete(plan);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Deleted successfully", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> UpdatePlan(long id, UpdatePlanDto updatePlanDto)
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

                // Search for plan
                Infrastructure.Model.Plan? plan = await _planRepository.FirstOrDefaultAsync(x => x.Id == id);
                if (plan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Plan does not exist", null);
                }

                // Update plan
                await _unitOfWork.BeginTransactionAsync();
                plan.Name = updatePlanDto.Name ?? plan.Name;
                plan.Description = updatePlanDto.Description ?? plan.Description;
                plan.Price = updatePlanDto.Price ?? plan.Price;
                plan.Discount = updatePlanDto.Discount ?? plan.Discount;
                plan.Screenshot = updatePlanDto.Screenshot ?? plan.Screenshot;
                plan.Visualisation = updatePlanDto.Visualisation ?? plan.Visualisation;
                plan.AIReport = updatePlanDto.AIReport ?? plan.AIReport;
                plan.UpdatedAt = DateTime.UtcNow.AddHours(1);

                _planRepository.Update(plan);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Plan updated successfully", new
                {
                    plan.Id,
                    plan.Name,
                    plan.Description,
                    plan.Price,
                    plan.Discount
                });
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetPlansAdmin()
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

                List<Infrastructure.Model.Plan> plans = await _planRepository.FindListAsync(x => !x.IsDeleted);
                Dictionary<string, long> planUsers = [];
                foreach (Infrastructure.Model.Plan plan in plans)
                {
                    List<Infrastructure.Model.User> users = await _userRepository.FindListAsync(x =>
                        !x.IsDeleted && x.PlanId == plan.Id);
                    planUsers.Add(plan.Name, users.Count);
                }

                return ResponseBuilder.Send(ResponseStatus.success, "Success", plans.Select(x => new
                {
                    x.Id,
                    x.Name,
                    Users = planUsers[x.Name],
                    x.Description,
                    x.Quota,
                    x.Concurrency,
                    x.Price,
                    DiscountedPrice = x.Price - (x.Price * (x.Discount / 100)),
                    x.Discount,
                    x.AIReport,
                    x.Screenshot,
                    x.Visualisation,
                    x.ConsoleApp
                }));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}
