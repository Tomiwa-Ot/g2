using G2.Infrastructure.Model;
using G2.Service.Messages.Dto.Receiving;

namespace G2.Service.Messages
{
    public interface IMessageService
    {
        Task<Response> SubmitMessage(AddMessageDto messageDto);
        Task<Response> UpdateMessage(long id, UpdateMessageDto messageDto);
        Task<Response> DeleteMessage(long id);
        Task<Response> GetMessage(long id);
        Task<Response> GetContactDetails();
        Task<Response> GetAllMessages(int page = 1, int limit = 10, bool? isRead = null, string? type = null, string? query = null);
    }
}
