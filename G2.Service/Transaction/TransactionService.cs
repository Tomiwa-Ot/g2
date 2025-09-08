using System.Linq.Expressions;
using AutoMapper;
using ErcasPay.Services.TransactionService.Response;
using FluentValidation.Results;
using G2.Infrastructure.Flutterwave.Checkout;
using G2.Infrastructure.Flutterwave.Models;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.PromoCode;
using G2.Infrastructure.Repository.Database.Transaction;
using G2.Infrastructure.Repository.Database.User;
using G2.Service.Helper;
using G2.Service.Transaction.Dto.Receiving;
using G2.Service.Transaction.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace G2.Service.Transaction
{
    public class TransactionService : ITransactionService
    {
        private IConfiguration _configuration;
        private readonly AddTransactionValidator _addTransactionValidator;
        private readonly ProfileHelper _profileHelper;
        private readonly IPlanRepository _planRepository;
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ErcasPay.Services.TransactionService.ITransactionService _ercaspay;
        private readonly IRequest _flutterwave;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(IConfiguration configuration,
                            AddTransactionValidator addTransactionValidator,
                            ProfileHelper profileHelper,
                            IPlanRepository planRepository,
                            IPromoCodeRepository promoCodeRepository,
                            ITransactionRepository transactionRepository,
                            IUserRepository userRepository,
                            IUnitOfWork unitOfWork,
                            IHttpContextAccessor httpContextAccessor,
                            ErcasPay.Services.TransactionService.ITransactionService ercasPay,
                            IRequest flutterwave,
                            IMapper mapper,
                            ILogger<TransactionService> logger)
        {
            _configuration = configuration;
            _addTransactionValidator = addTransactionValidator;
            _profileHelper = profileHelper;
            _planRepository = planRepository;
            _promoCodeRepository = promoCodeRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _ercaspay = ercasPay;
            _flutterwave = flutterwave;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Response> AddTransaction(AddTransactionDto addTransactionDto)
        {
            try
            {
                // Validate details
                ValidationResult validationResult = await _addTransactionValidator.ValidateAsync(addTransactionDto);
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

                await _unitOfWork.BeginTransactionAsync();

                // Fetch user
                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x
                    => x.Id == user.Id && x.IsVerified && !x.IsDisabled && !x.IsDeleted);
                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Search for plan
                Infrastructure.Model.Plan? plan = await _planRepository.FirstOrDefaultAsync(x
                    => x.Id == addTransactionDto.PlanId && !x.IsDeleted);
                if (plan == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }
                
                double amount = plan.Price - (plan.Price * (plan.Discount / 100));
                amount = addTransactionDto.Yearly ? amount * 12 : amount;

                // Apply promo code if it exists
                if (!string.IsNullOrEmpty(addTransactionDto.PromoCode))
                {
                    Infrastructure.Model.PromoCode? promoCode = await _promoCodeRepository.FirstOrDefaultAsync(x
                        => x.Code.Equals(addTransactionDto.PromoCode, StringComparison.OrdinalIgnoreCase));
                    if (promoCode == null)
                    {
                        return ResponseBuilder.Send(ResponseStatus.invalid_promo_code, "Promo code doesn't exist", null);
                    }
                    else if (DateTime.Today > promoCode.ExpiredAt || promoCode.UsageCount >= promoCode.UsageLimit)
                    {
                        return ResponseBuilder.Send(ResponseStatus.expired_promo_code, "Promo code has expired", null);
                    }

                    amount -= amount * (promoCode.Discount / 100);
                    promoCode.UsageCount = ++promoCode.UsageCount;
                    _promoCodeRepository.Update(promoCode);
                    await _unitOfWork.SaveChangesAsync();
                }
                

                if (addTransactionDto.Provider.Equals("ercaspay", StringComparison.OrdinalIgnoreCase))
                {
                    ErcasPay.Base.Request.Transaction payment = new()
                    {
                        amount = amount,
                        paymentMethods = "card",
                        currency = "USD",
                        customerEmail = user.Email,
                        customerName = account.Fullname,
                        description = $"G2 {plan.Name} Plan",
                        paymentReference = Guid.NewGuid().ToString()
                    };

                    InitiateTransactionResponse trxnResponse = (InitiateTransactionResponse)await _ercaspay.InitiateTransaction(payment);
                    if (!trxnResponse.RequestSuccessful)
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    Infrastructure.Model.Transaction newTransaction = new()
                    {
                        UserId = (long)user.Id,
                        Amount = amount,
                        Provider = "ercaspay",
                        CreatedAt = DateTime.UtcNow.AddHours(1),
                        UpdatedAt = DateTime.UtcNow.AddHours(1),
                        Reference = trxnResponse.ResponseBody.PaymentReference,
                        ProviderReference = trxnResponse.ResponseBody.TransactionReference,
                        PlanId = plan.Id,
                        IsYearly = addTransactionDto.Yearly,
                        Status = "Pending",
                        IsDeleted = false,
                    };

                    await _transactionRepository.AddAsync(newTransaction);
                    await _unitOfWork.CommitTransactionAsync();

                    return ResponseBuilder.Send(ResponseStatus.success, "", new
                    {
                        newTransaction.Id,
                        trxnResponse.ResponseBody.CheckoutUrl
                    });
                }
                else if (addTransactionDto.Provider.Equals("flutterwave", StringComparison.OrdinalIgnoreCase))
                {
                    Payment payment = new()
                    {
                        Amount = amount,
                        Tx_Ref = Guid.NewGuid().ToString(),
                        Currency = "USD",
                        Redirect_Url = _configuration.GetSection("Domain")["Url"] + "dashboard/transactions",
                        Customer = new Infrastructure.Flutterwave.Models.Customer()
                        {
                            Email = account.Email,
                            Name = account.Fullname
                        }
                    };


                    CheckoutResponse response = await _flutterwave.MakePayment(payment);
                    if (!response.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    Infrastructure.Model.Transaction newTransaction = new()
                    {
                        UserId = (long)user.Id,
                        Amount = amount,
                        Provider = "flutterwave",
                        CreatedAt = DateTime.UtcNow.AddHours(1),
                        UpdatedAt = DateTime.UtcNow.AddHours(1),
                        Reference = payment.Tx_Ref,
                        ProviderReference = "",
                        PlanId = plan.Id,
                        IsYearly = addTransactionDto.Yearly,
                        Status = "Pending",
                        IsDeleted = false,
                    };

                    await _transactionRepository.AddAsync(newTransaction);
                    await _unitOfWork.CommitTransactionAsync();

                    return ResponseBuilder.Send(ResponseStatus.success, "", new
                    {
                        newTransaction.Id,
                        response.Data.Link
                    });
                }
                
                await _unitOfWork.RollbackTransactionAsync();
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetTransactionById(long id)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Search for transacttion
                Infrastructure.Model.Transaction? transaction = await _transactionRepository.FirstOrDefaultAsync(x =>
                    (user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase) || x.UserId == user.Id)
                    && x.Id == id, includeProperties: "Plan");
                if (transaction == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Transaction does not exist", null);
                }

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    transaction.Id,
                    transaction.Amount,
                    transaction.Provider,
                    transaction.ProviderReference,
                    transaction.Reference,
                    transaction.PlanId,
                    transaction.Plan.Name,
                    transaction.Status,
                    transaction.CreatedAt
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetUserTransactions(int page = 1, int limit = 10)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                page = Math.Max(page, 1);
                limit = Math.Clamp(limit, 1, 100);
                int skip = (page - 1) * limit;

                // get total size
                List<Infrastructure.Model.Transaction> transactionsList = await _transactionRepository.FindListAsync(x =>
                    x.UserId == user.Id);

                // Search for user transactions
                List<Infrastructure.Model.Transaction> transactions = await _transactionRepository.FindListAsync(x =>
                    x.UserId == user.Id,
                    orderBy: q => q.OrderByDescending(e => e.Id),
                    skip: skip,
                    take: limit,
                    includes: u => u.Plan);

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    TotalSize = transactionsList.Count,
                    Items = transactions.Select(x => new
                    {
                        x.Id,
                        x.Amount,
                        x.Provider,
                        x.ProviderReference,
                        x.Reference,
                        x.PlanId,
                        x.Plan.Name,
                        x.Status,
                        x.IsYearly,
                        x.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> UpdateErcaspayTransaction(UpdateErcaspayTransactionDto updateTransactionDto)
        {
            try
            {
                // signature verification 
                string environment = _configuration.GetSection("ErcasPay")["Env"];
                if (environment.Equals("live", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext context = _httpContextAccessor.HttpContext;
                    if (context != null && context.Request.Headers.TryGetValue("signature", out var headerValue))
                    {
                        string signature = headerValue.ToString();
                        string expectedSignature = _configuration.GetSection("ErcasPay")["Signature"];
                        if (!expectedSignature.Equals(signature))
                        {
                            return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                        }
                    }
                }

                Infrastructure.Model.Transaction? transaction = await _transactionRepository.FirstOrDefaultAsync(x =>
                    x.ProviderReference == updateTransactionDto.Transaction_Reference
                    && x.Reference == updateTransactionDto.Payment_Reference
                    && x.Status.Equals("pending", StringComparison.OrdinalIgnoreCase), includeProperties: "User");
                if (transaction == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                // Update transaction
                await _unitOfWork.BeginTransactionAsync();
                if (updateTransactionDto.Status.Equals("successful", StringComparison.OrdinalIgnoreCase))
                {
                    VerifyTransactionResponse verifyTransaction = await _ercaspay.VerifyTransaction(updateTransactionDto.Transaction_Reference);
                    if (verifyTransaction.ResponseBody.Status.Equals("successful", StringComparison.OrdinalIgnoreCase))
                    {
                        transaction.User.PlanId = transaction.PlanId;
                        transaction.User.PlanExpiration = transaction.IsYearly ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);

                    }
                    transaction.Status = verifyTransaction.ResponseBody.Status;
                }
                else
                {
                    transaction.Status = updateTransactionDto.Status;
                }
                transaction.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _transactionRepository.Update(transaction);
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

        public async Task<Response> GetTransactions(int page = 1, int limit = 10, string? query = null)
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
                List<Infrastructure.Model.Transaction> transactionsList = await _transactionRepository.FindListAsync(x =>
                    !x.IsDeleted);

                List<Infrastructure.Model.Transaction> transactions = await _transactionRepository.FindListAsync(x =>
                    !x.IsDeleted &&
                    (string.IsNullOrEmpty(query)
                        || x.User.Fullname.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || x.User.Email.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || x.Provider.Equals(query, StringComparison.OrdinalIgnoreCase)
                        || x.Reference.Equals(query, StringComparison.OrdinalIgnoreCase)
                        || x.ProviderReference.Equals(query, StringComparison.OrdinalIgnoreCase)
                        || x.Status.Contains(query, StringComparison.OrdinalIgnoreCase)),
                    orderBy: q => q.OrderByDescending(r => r.Id),
                    skip: skip,
                    take: limit,
                    includes:  [u => u.User, u => u.Plan]);

                var response = transactions.Select(x => new
                {     
                    x.Id,
                    x.User.Email,
                    x.Amount,
                    x.PlanId,
                    x.Plan.Name,
                    x.Status,
                    x.CreatedAt
                }).ToList();
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    TotalSize = transactionsList.Count,
                    Items = response
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> UpdateFlutterwaveTransaction(UpdateFlutterwaveTransactionDto updateFlutterwave)
        {
            try
            {
                // verify if request is from flutterwave
                HttpContext context = _httpContextAccessor.HttpContext;
                if (context != null && context.Request.Headers.TryGetValue("verif-hash", out var headerValue))
                {
                    string hash = headerValue.ToString();
                    string expectedHash = _configuration.GetSection("Flutterwave")["Hash"];
                    if (!expectedHash.Equals(hash))
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    // search for transaction in db
                    Infrastructure.Model.Transaction? transaction = await _transactionRepository.FirstOrDefaultAsync(x 
                        => !x.IsDeleted && x.Provider.Equals("flutterwave", StringComparison.OrdinalIgnoreCase)
                            && x.Amount == updateFlutterwave.Data.Amount 
                            && x.Reference.Equals(updateFlutterwave.Data.Tx_Ref, StringComparison.OrdinalIgnoreCase), includeProperties: "User");

                    if (transaction == null)
                    {
                        return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                    }

                    await _unitOfWork.BeginTransactionAsync();

                    // verify transaction if it is successful
                    if (updateFlutterwave.Event.Equals("charge.completed", StringComparison.OrdinalIgnoreCase)
                        && updateFlutterwave.Data.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
                    {
                        TransactionVerificationResponse response = await _flutterwave.VerifyTransaction((long)updateFlutterwave.Data.Id);
                        if (response.Status.Equals("success", StringComparison.OrdinalIgnoreCase)
                            && response.Data.Status.Equals("successful", StringComparison.OrdinalIgnoreCase))
                        {
                            // update and save transaction
                            transaction.Status = response.Data.Status;
                            transaction.ProviderReference = response.Data.Flw_Ref;
                            transaction.UpdatedAt = DateTime.UtcNow.AddHours(1);
                            transaction.User.PlanId = transaction.PlanId;
                            transaction.User.PlanExpiration = transaction.IsYearly ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
                            _transactionRepository.Update(transaction);

                            await _unitOfWork.CommitTransactionAsync();
                            return ResponseBuilder.Send(ResponseStatus.success, "Success", null);
                        }
                    }

                    // mark transaction as failed
                    transaction.Status = "Failed";
                    transaction.ProviderReference = updateFlutterwave.Data.Flw_Ref;
                    transaction.UpdatedAt = DateTime.UtcNow.AddHours(1);
                    _transactionRepository.Update(transaction);
                    await _unitOfWork.CommitTransactionAsync();

                    return ResponseBuilder.Send(ResponseStatus.success, "Success", null);
                }
                
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> VerifyFlutterwave(VerifyFlutterwave verifyFlutterwave)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Infrastructure.Model.Transaction? transaction = await _transactionRepository.FirstOrDefaultAsync(x
                    => !x.IsDeleted && x.Reference.Equals(verifyFlutterwave.TransactionReference, StringComparison.OrdinalIgnoreCase)
                    && x.UserId == user.Id, includeProperties: "User");
                if (transaction == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }
                
                TransactionVerificationResponse response = await _flutterwave.VerifyTransaction(verifyFlutterwave.TransactionId);
                if (response.Status.Equals("success", StringComparison.OrdinalIgnoreCase)
                    && response.Data.Status.Equals("successful", StringComparison.OrdinalIgnoreCase))
                {
                    await _unitOfWork.BeginTransactionAsync();
                    
                    // update and save transaction
                    transaction.Status = response.Data.Status;
                    transaction.ProviderReference = verifyFlutterwave.TransactionId.ToString();
                    transaction.UpdatedAt = DateTime.UtcNow.AddHours(1);
                    transaction.User.PlanId = transaction.PlanId;
                    transaction.User.PlanExpiration = transaction.IsYearly ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
                    _transactionRepository.Update(transaction);

                    await _unitOfWork.CommitTransactionAsync();
                    return ResponseBuilder.Send(ResponseStatus.success, "Success", null);
                }
             
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}
