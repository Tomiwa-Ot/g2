using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using G2.Infrastructure.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;

namespace G2.Infrastructure.Repository.MessageQueue
{
    public class MessageQueue : IMessageQueue
    {
        private readonly IChannel _channel;
        private readonly IConfiguration _configuration;
        private string _queueName = "g2_jobs";

        public MessageQueue(IChannel channel,
                IConfiguration configuration)
        {
            _channel = channel;
            _configuration = configuration;
            _queueName = _configuration.GetSection("RabbitMQ")["Queue"];
        }

        public async Task Dequeue(Func<(Job, CancellationToken), Task> onMessage, CancellationToken token)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                Job job = JsonSerializer.Deserialize<Job>(body);
                // onMessage.Invoke((job, token));
                await onMessage((job, token));
                
                // return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: consumer);
        }

        public async Task Enqueue(Job job)
        {
            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                body: body
            );
        }
    }
}