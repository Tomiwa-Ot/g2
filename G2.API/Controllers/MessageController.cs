using G2.Infrastructure.Model;
using G2.Service.Messages;
using G2.Service.Messages.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/message")]
    [ApiController]
    public class MessageController: ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        
        [HttpGet]
        public async Task<Response> GetContactDetails()
        {
           return await _messageService.GetContactDetails();
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<Response> GetAllMessages([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? type = null, [FromQuery] string? query = null, [FromQuery] bool? isRead = false)
        {
            return await _messageService.GetAllMessages(page, limit, isRead, type, query);
        }

        [HttpPost]
        [Authorize]
        public async Task<Response> SubmitMessage(AddMessageDto messageDto)
        {
            return await _messageService.SubmitMessage(messageDto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<Response> UpdateMessage(long id, UpdateMessageDto messageDto)
        {
            return await _messageService.UpdateMessage(id, messageDto);
        }
    }
}
