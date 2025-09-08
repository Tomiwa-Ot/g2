using AutoMapper;
using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Message;
using G2.Service.Helper;
using G2.Service.Messages.Dto.Receiving;
using G2.Service.Messages.Dto.Transfer;
using G2.Service.Messages.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace G2.Service.Messages
{
    public class MessageService : IMessageService
    {
        private readonly AddMessageValidator _addMessageValidator;
        private readonly UpdateMessageValidator _updateMessageValidator;
        private readonly ProfileHelper _profileHelper;
        private readonly IMessageRepository _messageRepository;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageService> _logger;

        public MessageService(AddMessageValidator addMessageValidator,
                        UpdateMessageValidator updateMessageValidator,
                        ProfileHelper profileHelper,
                        IMessageRepository messageRepository,
                        IConfiguration configuration,
                        IUnitOfWork unitOfWork,
                        IMapper mapper,
                        ILogger<MessageService> logger)
        {
            _addMessageValidator = addMessageValidator;
            _updateMessageValidator = updateMessageValidator;
            _profileHelper = profileHelper;
            _messageRepository = messageRepository;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }
        
        public async Task<Response> GetContactDetails()
        {
            try
            {
            	return ResponseBuilder.Send(ResponseStatus.success, "Success", new
            	{
            		Email = _configuration.GetSection("Contact")["Email"],
            		Linkedin = _configuration.GetSection("Contact")["Linkedin"],
            		Twitter = _configuration.GetSection("Contact")["Twitter"],
            	});
            }
            catch (Exception e)
            {
            	_logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> SubmitMessage(AddMessageDto messageDto)
        {
            try
            {
                // Validate details
                ValidationResult validationResult = await _addMessageValidator.ValidateAsync(messageDto);
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

                Message message = _mapper.Map<Message>(messageDto);
                message.UserId = (long)user.Id;
                message.Unread = true;
                message.CreatedAt = DateTime.UtcNow.AddHours(1);
                message.UpdatedAt = DateTime.UtcNow.AddHours(1);

                await _messageRepository.AddAsync(message);
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

        public async Task<Response> UpdateMessage(long id, UpdateMessageDto messageDto)
        {
            try
            {
                // Validate details
                ValidationResult validationResult = await _updateMessageValidator.ValidateAsync(messageDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Get user details
                // user role should be admin
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Message? message = await _messageRepository.FirstOrDefaultAsync(x =>
                    x.Id == id && !x.IsDeleted);
                if (message == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Message not found", null);
                }

                await _unitOfWork.BeginTransactionAsync();
                message.Unread = messageDto.Unread;
                message.UpdatedAt = DateTime.UtcNow.AddHours(1);
                _messageRepository.Update(message);

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

        public async Task<Response> DeleteMessage(long id)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("super-admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Message? message = await _messageRepository.FirstOrDefaultAsync(x 
                    => x.Id == id && !x.IsDeleted);
                if (message == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Message not found", null);
                }

                await _unitOfWork.BeginTransactionAsync();
                message.IsDeleted = true;
                _messageRepository.Delete(message);
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
        public async Task<Response> GetMessage(long id)
        {
            try
            {
                // user role should be admin
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Message? message = await _messageRepository.FirstOrDefaultAsync(x =>
                    x.Id == id && !x.IsDeleted, includeProperties: "User");
                if (message == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Message not found", null);
                }

                // map and send message dto
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new MessageDto()
                {
                    Id = message.Id,
                    UserId = (long)user.Id,
                    Email = message.User.Email,
                    Fullname = message.User.Fullname,
                    Type = message.Type,
                    Title = message.Title,
                    Body = message.Body,
                    JobId = message.JobId,
                    TransactionId = message.TransactionId,
                    Unread = message.Unread,
                    CreatedAt = message.CreatedAt
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
        public async Task<Response> GetAllMessages(int page = 1, int limit = 10, bool? isRead = null, string? type = null, string? query = null)
        {
            try
            {
                // user role should be admin
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                page = Math.Max(page, 1);
                limit = Math.Clamp(limit, 1, 100);
                int skip = (page - 1) * limit;

                List<Message> messages = await _messageRepository.FindListAsync(x
                    => (!isRead.HasValue || x.Unread == isRead.Value) &&
                        (string.IsNullOrEmpty(type) || x.Type.Equals(type, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(query) || x.User.Fullname.Equals(query, StringComparison.OrdinalIgnoreCase)
                        || x.User.Email.Equals(query, StringComparison.OrdinalIgnoreCase) || x.Title.Equals(query, StringComparison.OrdinalIgnoreCase)),
                        orderBy: q => q.OrderByDescending(r => r.Id),
                        skip: skip,
                        take: limit,
                        includes: u => u.User);

                return ResponseBuilder.Send(ResponseStatus.success, "Success", messages.Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Body,
                    x.User.Fullname,
                    x.User.Email,
                    x.TransactionId,
                    x.JobId,
                    x.Unread,
                    x.CreatedAt
                }).ToList());
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}
