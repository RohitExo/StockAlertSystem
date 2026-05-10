using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using StockAlert.API.Models;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace StockAlert.API.Services
{
    public class MessageProducer:IMessageProducer
    {
        public async void SendingMessage<T>(T message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/"
            };
            using var _connection = await factory.CreateConnectionAsync();
            using var _channel = await _connection.CreateChannelAsync();

            // Implement for dead letter - rejected messages 
            var dlxName = "stock-alert-dlx";
            var dlqName = "stock-alert-dead-letter";
            await _channel.ExchangeDeclareAsync(exchange: dlxName, type: ExchangeType.Direct);

            await _channel.QueueDeclareAsync(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(queue: dlqName, exchange: dlxName, routingKey: "dead-letter");

            var mainArgs = new Dictionary<string, object?>
            {
                { "x-queue-type", "quorum" },
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", "dead-letter" }
            };


            await _channel.QueueDeclareAsync(queue: "stock-alert", durable: true, exclusive: false, autoDelete: false,
                arguments: mainArgs);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            string? correlationId = (message is Alert alert) ? alert.CorrelationId.ToString() : null;

            await _channel.BasicPublishAsync(
                exchange: string.Empty, 
                routingKey: "stock-alert", 
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId
                }, 
                body: body
                );
        }
    }
}
