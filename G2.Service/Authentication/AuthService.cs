using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using AutoMapper;
using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository;
using G2.Infrastructure.Repository.Database.AccountVerification;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.Referral;
using G2.Infrastructure.Repository.Database.Role;
using G2.Infrastructure.Repository.Database.User;
using G2.Service.Authentication.Dto.Receiving;
using G2.Service.Authentication.Validation;
using G2.Service.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using StackExchange.Redis;

namespace G2.Service.Authentication
{
    public partial class AuthService : IAuthService
    {
        private readonly GoogleValidator _googleValidator;
        private readonly GithubValidator _githubValidator;
        private readonly ForgotPasswordValidator _forgotPasswordValidator;
        private readonly LoginValidator _loginValidator;
        private readonly PasswordResetValidator _passwordResetValidator;
        private readonly RegistrationValidator _registrationValidator;
        private readonly TokenRenewalValidator _tokenRenewalValidator;
        private readonly EmailHelper _emailHelper;
        private readonly JwtHelper _jwtHelper;
        private readonly ProfileHelper _profileHelper;
        private readonly IMemoryCache _memoryCache;
        private readonly IAccountVerificationRepository _accountVerificationRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IReferralRepository _referralRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private const long MaxLoginAttempts = 5;
        private const long LockoutMinutes = 10;

        public AuthService(GoogleValidator googleValidator,
                        GithubValidator githubValidator,
                        ForgotPasswordValidator forgotPasswordValidator,
                        LoginValidator loginValidator,
                        PasswordResetValidator passwordResetValidator,
                        RegistrationValidator registrationValidator,
                        TokenRenewalValidator tokenRenewalValidator,
                        EmailHelper emailHelper,
                        JwtHelper jwtHelper,
                        ProfileHelper profileHelper,
                        IMemoryCache memoryCache,
                        IAccountVerificationRepository accountVerificationRepository,
                        IPlanRepository planRepository,
                        IReferralRepository referralRepository,
                        IRoleRepository roleRepository,
                        IUserRepository userRepository,
                        IUnitOfWork unitOfWork,
                        IConfiguration configuration,
                        IMapper mapper,
                        ILogger<AuthService> logger)
        {
            _googleValidator = googleValidator;
            _githubValidator = githubValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _loginValidator = loginValidator;
            _passwordResetValidator = passwordResetValidator;
            _registrationValidator = registrationValidator;
            _tokenRenewalValidator = tokenRenewalValidator;
            _jwtHelper = jwtHelper;
            _emailHelper = emailHelper;
            _profileHelper = profileHelper;
            _memoryCache = memoryCache;
            _accountVerificationRepository = accountVerificationRepository;
            _planRepository = planRepository;
            _referralRepository = referralRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Response> Login(LoginDto loginDto, bool mobile)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _loginValidator.ValidateAsync(loginDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Check if email exists
                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x
                     => x.Email.Equals(loginDto.Email, StringComparison.OrdinalIgnoreCase)
                        && !x.IsDisabled && !x.IsDeleted && x.IsVerified, includeProperties: "Role");
                if (user == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Invalid email or password", null);
                }

                // Check if account is locked
                RedisValue isAccountLocked = await _memoryCache.GetString($"login:lockout:{user.Id}");
                if (!isAccountLocked.IsNullOrEmpty)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Invalid email or password", null);
                }

                // Verify password
                if (!_profileHelper.VerifyPassword(user.PasswordHash, user.Salt, loginDto.Password))
                {
                    // Track failed login attempts
                    RedisValue failCount = await _memoryCache.GetString($"login:fail:{user.Id}");
                    if (failCount.IsNullOrEmpty)
                    {
                        await _memoryCache.SetString($"login:fail:{user.Id}", "1", TimeSpan.FromMinutes(LockoutMinutes));
                    }
                    else
                    {
                        long count = await _memoryCache.IncrementString($"login:fail:{user.Id}");
                        // Lock account
                        if (count >= MaxLoginAttempts)
                        {
                            await _memoryCache.SetString($"login:lockout:{user.Id}", "1", TimeSpan.FromMinutes(LockoutMinutes));
                            _emailHelper.SendMail(EmailType.account_locked, user.Email, new
                            {
                                user.Fullname,
                                Duration = "10 minutes"
                            });
                        }
                    }

                    return ResponseBuilder.Send(ResponseStatus.failure, "Invalid email or password", null);
                }

                if (mobile && !user.Role.Name.Contains("admin", StringComparison.CurrentCultureIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Invalid email or password", null);
                }

                // Generate access and refresh token
                string accessToken = _jwtHelper.GenerateAccessToken(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Fullname),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.Name),
                ]);
                string refreshToken = _jwtHelper.GenerateRefreshToken();

                // Save refresh token
                await _unitOfWork.BeginTransactionAsync();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Login successful", new
                {
                    user.Fullname,
                    user.Email,
                    user.AuthToken,
                    user.ReferralCode,
                    user.Provider,
                    user.PlanId,
                    user.PlanExpiration,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> Register(RegisterDto registerDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _registrationValidator.ValidateAsync(registerDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Check if email is registered and verified
                Infrastructure.Model.User? existingUser = await _userRepository.FirstOrDefaultAsync(x
                    => x.Email.Equals(registerDto.Email, StringComparison.OrdinalIgnoreCase));
                await _unitOfWork.BeginTransactionAsync();
                if (existingUser != null && !existingUser.IsVerified && existingUser.CreatedAt.AddHours(1) < DateTime.UtcNow)
                {
                    existingUser.IsDeleted = true;
                    _userRepository.Update(existingUser);
                }
                else if (existingUser != null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Get free plan
                Infrastructure.Model.Plan? freePlan = await _planRepository.FirstOrDefaultAsync(x
                    => x.Name.Equals("free", StringComparison.OrdinalIgnoreCase) &&
                        x.Price == 0.00);
                if (freePlan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Select user role
                Infrastructure.Model.Role? userRole = await _roleRepository.FirstOrDefaultAsync(x
                    => x.Name.Equals("user", StringComparison.OrdinalIgnoreCase));
                if (userRole == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Hash password
                var (salt, hash) = _profileHelper.HashPassword([], registerDto.Password);

                // Save user to database
                Infrastructure.Model.User newUser = _mapper.Map<Infrastructure.Model.User>(registerDto);
                newUser.PasswordHash = hash;
                newUser.Salt = Convert.ToBase64String(salt);
                newUser.AuthToken = Guid.NewGuid().ToString();
                newUser.CreatedAt = DateTime.UtcNow.AddHours(1);
                newUser.UpdatedAt = DateTime.UtcNow.AddHours(1);
                newUser.RoleId = userRole.Id;
                newUser.ReferralCode = _profileHelper.GenerateReferralCode(7);
                newUser.IsDeleted = false;
                newUser.IsDisabled = true;
                newUser.IsVerified = false;
                newUser.PlanId = freePlan.Id;
                newUser.ReferralQuota = 0;

                Infrastructure.Model.User createdUser = await _userRepository.AddAsync(newUser);
                await _unitOfWork.SaveChangesAsync();

                if (!string.IsNullOrEmpty(registerDto.ReferralCode))
                {
                    Infrastructure.Model.User? referrer = await _userRepository.FirstOrDefaultAsync(x
                        => x.ReferralCode.Equals(registerDto.ReferralCode,
                        StringComparison.OrdinalIgnoreCase));
                    if (referrer != null)
                    {
                        await _referralRepository.AddAsync(new Referral
                        {
                            ReferredId = createdUser.Id,
                            ReferrerId = referrer.Id,
                            CreatedAt = DateTime.UtcNow.AddHours(1)
                        });
                        await _unitOfWork.SaveChangesAsync();
                        referrer.ReferralQuota = ++referrer.ReferralQuota;
                        referrer.UpdatedAt = DateTime.UtcNow.AddHours(1);
                        _userRepository.Update(referrer);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                // Generate code
                string token = _profileHelper.GenerateReferralCode(6);
                DateTime createdAt = DateTime.UtcNow.AddHours(1);

                // Add code to code table 
                AccountVerification verification = new()
                {
                    UserId = createdUser.Id,
                    CreatedAt = createdAt,
                    Token = token,
                    Expiration = createdAt.AddHours(1)
                };
                await _accountVerificationRepository.AddAsync(verification);
                await _unitOfWork.SaveChangesAsync();

                _emailHelper.SendMail(EmailType.email_verification, createdUser.Email, new
                {
                    createdUser.Fullname,
                    Token = token.ToUpper()
                });
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Registration successful", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _forgotPasswordValidator.ValidateAsync(forgotPasswordDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                await _unitOfWork.BeginTransactionAsync();

                // Check if user exists and didn't use an external provider
                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x =>
                    x.Email.Equals(forgotPasswordDto.Email, StringComparison.OrdinalIgnoreCase)
                    && x.IsVerified && !x.IsDisabled && !x.IsDeleted && string.IsNullOrEmpty(x.Provider));
                if (user != null)
                {
                    // Generate & send reset token
                    string resetToken = _profileHelper.GenerateReferralCode(6);
                    user.ResetToken = resetToken;
                    user.ResetTokenExpiration = DateTime.UtcNow.AddMinutes(10);
                    user.UpdatedAt = DateTime.UtcNow.AddHours(1);
                    _userRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync();

                    _emailHelper.SendMail(EmailType.forgot_password, user.Email, new
                    {
                        user.Fullname,
                        ResetToken = resetToken.ToUpper(),
                        Expiration = "10 minutes"
                    });
                }
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "You'll receive an email if you have an account with us", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> RenewToken(RenewTokenDto renewTokenDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _tokenRenewalValidator.ValidateAsync(renewTokenDto);
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

                // Validate refresh token & check if it has expired
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x =>
                    x.Id == user.Id && x.IsVerified && !x.IsDisabled && !x.IsDeleted, includeProperties: "Role");
                if (!account.RefreshToken.Equals(renewTokenDto.RefreshToken)
                    || account.RefreshTokenExpiration.Value.AddDays(7) < DateTime.UtcNow)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Generate new access token
                string newAccessToken = _jwtHelper.GenerateAccessToken(
                [
                    new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                    new Claim(ClaimTypes.Name, account.Fullname),
                    new Claim(ClaimTypes.Email, account.Email),
                    new Claim(ClaimTypes.Role, account.Role.Name),
                ]
                );

                return ResponseBuilder.Send(ResponseStatus.success, "Successful", new
                {
                    AccessToken = newAccessToken
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _passwordResetValidator.ValidateAsync(resetPasswordDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                await _unitOfWork.BeginTransactionAsync();

                // Search for user with reset token
                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x =>
                    x.Email.Equals(resetPasswordDto.Email, StringComparison.OrdinalIgnoreCase) &&
                    x.ResetToken.Equals(resetPasswordDto.ResetToken, StringComparison.OrdinalIgnoreCase)
                    && x.ResetTokenExpiration.Value.AddMinutes(10) < DateTime.UtcNow.AddHours(1));
                if (user == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Update user password
                var (salt, hash) = _profileHelper.HashPassword([], resetPasswordDto.Password);
                user.Salt = Convert.ToBase64String(salt);
                user.PasswordHash = hash;
                user.ResetToken = null;
                user.ResetTokenExpiration = null;
                user.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _emailHelper.SendMail(EmailType.password_reset, user.Email, new
                {
                    user.Fullname,
                    UpdatedAt = user.UpdatedAt.ToString()
                });
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Password reset successful", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> VerifyAccount(VerifyAccountDto verifyAccountDto)
        {
            try
            {
                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x
                    => x.Email.Equals(verifyAccountDto.Email, StringComparison.OrdinalIgnoreCase)
                    && !x.IsVerified);
                if (user == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Check if account is locked
                RedisValue isAccountLocked = await _memoryCache.GetString($"verification:lockout:{user.Id}");
                if (!isAccountLocked.IsNullOrEmpty)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                AccountVerification? verificationRequest = await _accountVerificationRepository.LastOrDefaultAsync(x
                    => x.UserId == user.Id && !x.UsedAt.HasValue
                    && x.Token.Equals(verifyAccountDto.Token, StringComparison.OrdinalIgnoreCase)
                    && DateTime.UtcNow.AddHours(1) < x.Expiration.AddHours(1),
                    orderBy: x => x.OrderByDescending(y => y.Id));
                if (verificationRequest == null)
                {
                    // Track failed login attempts
                    RedisValue failCount = await _memoryCache.GetString($"verification:fail:{user.Id}");
                    if (failCount.IsNullOrEmpty)
                    {
                        await _memoryCache.SetString($"verification:fail:{user.Id}", "1", TimeSpan.FromMinutes(LockoutMinutes));
                    }
                    else
                    {
                        long count = await _memoryCache.IncrementString($"verification:fail:{user.Id}");
                        // Lock account
                        if (count >= MaxLoginAttempts)
                        {
                            await _memoryCache.SetString($"verification:lockout:{user.Id}", "1", TimeSpan.FromMinutes(LockoutMinutes));
                        }
                    }
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                await _unitOfWork.BeginTransactionAsync();
                verificationRequest.UsedAt = DateTime.UtcNow.AddHours(1);
                user.IsVerified = true;
                user.IsDisabled = false;
                user.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _accountVerificationRepository.Update(verificationRequest);
                await _unitOfWork.SaveChangesAsync();
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Account verified", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> ResendCode(ResendCodeDto resendCodeDto)
        {
            try
            {
                // Search for user
                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x
                    => x.Email.Equals(resendCodeDto.Email, StringComparison.OrdinalIgnoreCase)
                    && !x.IsVerified && !x.IsDisabled && !x.IsDeleted);

                if (user == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Generate code
                string token = _profileHelper.GenerateReferralCode(6);
                DateTime createdAt = DateTime.UtcNow.AddHours(1);

                // Add code to code table 
                await _unitOfWork.BeginTransactionAsync();
                AccountVerification verification = new()
                {
                    UserId = user.Id,
                    CreatedAt = createdAt,
                    Token = token,
                    Expiration = createdAt.AddMinutes(30)
                };
                await _accountVerificationRepository.AddAsync(verification);
                await _unitOfWork.SaveChangesAsync();

                _emailHelper.SendMail(EmailType.email_verification, user.Email, new { Token = token.ToUpper() });
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

        public async Task<Response> SignInWithGithub(GithubDto githubDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _githubValidator.ValidateAsync(githubDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Fetch user details from github
                HttpClient client = new();
                Dictionary<string, string> parameters = new()
                {
                    { "client_id", _configuration.GetSection("Github")["ClientId"] },
                    { "client_secret", _configuration.GetSection("Github")["ClientSecret"] },
                    { "code", githubDto.Code },
                };
                FormUrlEncodedContent body = new FormUrlEncodedContent(parameters);
                HttpResponseMessage response = await client.PostAsync("https://github.com/login/oauth/access_token", body);
                string responseContent = await response.Content.ReadAsStringAsync();
                string githubAccessToken = HttpUtility.ParseQueryString(responseContent)["access_token"];
                GitHubClient gitHubClient = new(new ProductHeaderValue("g2"));
                Credentials credentials = new(githubAccessToken);
                gitHubClient.Credentials = credentials;
                Octokit.User account = await gitHubClient.User.Current();

                // Create user if it doesn't exist
                await _unitOfWork.BeginTransactionAsync();

                Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x =>
                    x.Email.Equals(account.Email, StringComparison.OrdinalIgnoreCase), includeProperties: "Role");
                if (user == null)
                {
                    // Get free plan
                    Infrastructure.Model.Plan? freePlan = await _planRepository.FirstOrDefaultAsync(x
                        => x.Name.Equals("free", StringComparison.OrdinalIgnoreCase) &&
                            x.Price == 0.00);
                    if (freePlan == null)
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    // Select user role
                    Infrastructure.Model.Role? userRole = await _roleRepository.FirstOrDefaultAsync(x
                        => x.Name.Equals("user", StringComparison.OrdinalIgnoreCase));
                    if (userRole == null)
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    user = new Infrastructure.Model.User()
                    {
                        Fullname = MyRegex().Replace(account.Name, ""),
                        Email = account.Email,
                        Provider = "github",
                        AuthToken = Guid.NewGuid().ToString(),
                        RoleId = userRole.Id,
                        ReferralCode = _profileHelper.GenerateReferralCode(7),
                        PlanId = freePlan.Id,
                        ReferralQuota = 0,
                        IsVerified = true,
                        IsDisabled = false,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow.AddHours(1),
                        UpdatedAt = DateTime.UtcNow.AddHours(1),
                    };

                    await _userRepository.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                    user = await _userRepository.FirstOrDefaultAsync(x =>
                        x.Email.Equals(account.Email, StringComparison.OrdinalIgnoreCase), includeProperties: "Role");
                }

                // Generate access and refresh token
                string accessToken = _jwtHelper.GenerateAccessToken(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Fullname),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.Name),
                ]);
                string refreshToken = _jwtHelper.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Login successful", new
                {
                    user.Fullname,
                    user.Email,
                    user.AuthToken,
                    user.ReferralCode,
                    user.PlanId,
                    user.PlanExpiration,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> SignInWithGoogle(GoogleDto googleDto)
        {
            try
            {
                // // Validate input
                // ValidationResult validationResult = await _googleValidator.ValidateAsync(googleDto);
                // if (!validationResult.IsValid)
                // {
                //     return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                // }

                // // Fetch user details from google
                // HttpClient httpClient = new();
                // HttpResponseMessage response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/userinfo?access_token={accessToken}");
                // string content = await response.Content.ReadAsStringAsync();

                // // Create user if it doesn't exist
                // await _unitOfWork.BeginTransactionAsync();

                // Infrastructure.Model.User? user = await _userRepository.FirstOrDefaultAsync(x =>
                //     x.Email.Equals(account.Email, StringComparison.OrdinalIgnoreCase));
                // if (user == null)
                // {
                //     // Get free plan
                //     Infrastructure.Model.Plan? freePlan = await _planRepository.FirstOrDefaultAsync(x
                //         => x.Name.Equals("free", StringComparison.OrdinalIgnoreCase) &&
                //             x.Price == 0.00);
                //     if (freePlan == null)
                //     {
                //         return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                //     }

                //     // Select user role
                //     Infrastructure.Model.Role? userRole = await _roleRepository.FirstOrDefaultAsync(x
                //         => x.Name.Equals("user", StringComparison.OrdinalIgnoreCase));
                //     if (userRole == null)
                //     {
                //         return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                //     }

                //     user = new Infrastructure.Model.User()
                //     {
                //         Fullname = account.Name,
                //         Email = account.Email,
                //         Provider = "github",
                //         AuthToken = Guid.NewGuid().ToString(),
                //         RoleId = userRole.Id,
                //         ReferralCode = _profileHelper.GenerateReferralCode(7),
                //         PlanId = freePlan.Id,
                //         ReferralQuota = 0,
                //         IsVerified = true,
                //         IsDisabled = false,
                //         IsDeleted = false,
                //         CreatedAt = DateTime.UtcNow.AddHours(1),
                //         UpdatedAt = DateTime.UtcNow.AddHours(1),
                //     };

                //     user = await _userRepository.AddAsync(user);
                //     await _unitOfWork.SaveChangesAsync();
                // }

                // // Generate access and refresh token
                // string accessToken = _jwtHelper.GenerateAccessToken(
                // [
                //     new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                //     new Claim(ClaimTypes.Name, user.Fullname),
                //     new Claim(ClaimTypes.Email, user.Email),
                //     new Claim(ClaimTypes.Role, user.Role.Name),
                // ]);
                // string refreshToken = _jwtHelper.GenerateRefreshToken();

                // // Save refresh token
                // user.RefreshToken = refreshToken;
                // user.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
                // _userRepository.Update(user);
                // await _unitOfWork.SaveChangesAsync();
                // await _unitOfWork.CommitTransactionAsync();

                // return ResponseBuilder.Send(ResponseStatus.success, "Login successful", content);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
            catch (Exception e)
            {
                // await _unitOfWork.RollbackTransactionAsync();
                // _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        [GeneratedRegex(@"[^a-zA-Z\s]")]
        private static partial Regex MyRegex();
    }
}
