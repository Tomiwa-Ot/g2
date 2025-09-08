using G2.Infrastructure.Model;

namespace G2.Infrastructure.Repository.MessageQueue
{
    public interface IMessageQueue
    {
        Task Enqueue(Job job);
        Task Dequeue(Func<(Job, CancellationToken), Task> onMessage, CancellationToken token);
    }
}