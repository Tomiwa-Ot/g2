using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.User;
using G2.Service.User.Validation;
using G2.Service.Helper;
using G2.Service.User.Dto.Receiving;
using Microsoft.Extensions.Logging;
using G2.Infrastructure.Repository.Database.Referral;
using Microsoft.EntityFrameworkCore;
using G2.Infrastructure.Repository.Database.Job;

namespace G2.Service.User
{
    public class UserService: IUserService
    {
        private readonly PasswordResetValidator _passwordResetValidator;
        private readonly ProfileHelper _profileHelper;
        private readonly IJobRepository _jobRepository;
        private readonly IReferralRepository _referralRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(ProfileHelper profileHelper,
                        PasswordResetValidator passwordResetValidator,
                        IJobRepository jobRepository,
                        IReferralRepository referralRepository,
                        IUserRepository userRepository,
                        IUnitOfWork unitOfWork,
                        ILogger<UserService> logger)
        {
            _profileHelper = profileHelper;
            _passwordResetValidator = passwordResetValidator;
            _jobRepository = jobRepository;
            _referralRepository = referralRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Response> RegenerateAuthToken()
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Search for user
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x => x.Id == user.Id
                    && !x.IsDisabled && !x.IsDeleted && x.IsVerified);

                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Generate new auth token & save to database
                await _unitOfWork.BeginTransactionAsync();

                string authToken = Guid.NewGuid().ToString();
                account.AuthToken = authToken;
                account.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _userRepository.Update(account);

                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new 
                {
                    account.AuthToken
                });
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
        
        public async Task<Response> ResetPassword(PasswordResetDto passwordResetDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _passwordResetValidator.ValidateAsync(passwordResetDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Search for user
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x => x.Id == user.Id
                    && !x.IsDisabled && !x.IsDeleted && x.IsVerified && string.IsNullOrEmpty(x.Provider));

                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Hash password
                var (salt, hash) = _profileHelper.HashPassword([], passwordResetDto.Password);

                await _unitOfWork.BeginTransactionAsync();
                account.PasswordHash = hash;
                account.Salt = Convert.ToBase64String(salt);
                account.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _userRepository.Update(account);

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

        public async Task<Response> UpdateUser(UpdateUserDto updateUserDto)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // name validation
                if (!string.IsNullOrEmpty(updateUserDto.Fullname) && !updateUserDto.Fullname.All(c => char.IsLetter(c) || c == ' '))
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Name cannot contain numbers", null);
                }

                // Search for user
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x
                    => x.Id == user.Id && !x.IsDeleted && x.IsVerified && !x.IsDisabled);
                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Update account
                await _unitOfWork.BeginTransactionAsync();
                
                account.Fullname = updateUserDto.Fullname ?? account.Fullname;
                account.Email = string.IsNullOrEmpty(account.Provider) ? (updateUserDto.Email ?? account.Email) : account.Email;
                account.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _userRepository.Update(account);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Account updated", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetReferrals()
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                List<Referral> referrals = await _referralRepository.FindListWithIncludeAsync(x => 
                    x.ReferrerId == user.Id && x.Referred.IsVerified, include: x => x.Include(r => r.Referred));

                // filter response
                return ResponseBuilder.Send(ResponseStatus.success, "Success", referrals);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetMyProfile()
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Fetch user details
                Infrastructure.Model.User? userDetails = await _userRepository.FirstOrDefaultAsync(x => 
                    x.Id == user.Id && x.IsVerified && !x.IsDeleted && !x.IsDisabled, includeProperties: "Plan");
                if (userDetails == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Used for calculating daily used quota
                List<Job> jobs = await _jobRepository.FindListAsync(x => 
                    x.UserId == user.Id && x.CreatedAt.Date == DateTime.Today);
                
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    PlanId = userDetails.Plan.Id,
                    PlanName = userDetails.Plan.Name,
                    Quota = userDetails.Plan.Quota + userDetails.ReferralQuota,
                    QuotaUsed = jobs.Count,
                    userDetails.PlanExpiration,
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> DeactivateAccount(bool? disable = true)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Search for user
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x
                    => x.Id == user.Id && !x.IsDeleted && x.IsVerified && !x.IsDisabled);
                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Disable acount
                await _unitOfWork.BeginTransactionAsync();
                account.IsDisabled = disable.Value;
                account.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _userRepository.Update(account);
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Account updated", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetUsers(int page = 1, int limit = 10, string? provider = null, string? query = null)
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

                // get total size
                List<Infrastructure.Model.User> usersList = await _userRepository.FindListAsync(x =>
                    !x.IsDeleted);

                List<Infrastructure.Model.User> users = await _userRepository.FindListAsync(x =>
                    !x.IsDeleted &&
                    (string.IsNullOrEmpty(provider) || x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(query) || x.Fullname.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(query, StringComparison.OrdinalIgnoreCase)),
                    orderBy: q => q.OrderByDescending(r => r.Id),
                    skip: skip,
                    take: limit);

                var response = users.Select(x => new
                {
                    TotalSize = usersList.Count,
                    x.Id,
                    x.Fullname,
                    x.Email,
                    x.CreatedAt
                }).ToList();
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    TotalSize = usersList.Count,
                    Items = response
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
