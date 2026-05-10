using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

Console.WriteLine("Welcome to the Notification Service");

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

var mainArgs = new Dictionary<string, object?> {
    { "x-queue-type", "quorum" },
    { "x-dead-letter-exchange", "stock-alert-dlx" },
    { "x-dead-letter-routing-key", "dead-letter" }
};

await _channel.QueueDeclareAsync(queue:"stock-alert",durable:true,exclusive:false,autoDelete:false,arguments:mainArgs);
var consumer = new AsyncEventingBasicConsumer(_channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    try
    {
        if (message.Contains("Error")) throw new Exception("Simulated processing failure");

        Console.WriteLine($"[StockAlert] Received: {message}");
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DLQ Trigger] Failing message: {ex.Message}");
        // Move to Dead letter queue if failed
        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
    }
};

await _channel.BasicConsumeAsync(queue: "stock-alert", autoAck: false, consumer: consumer);
Console.WriteLine("Consuming Stock Alerts... Press enter to exit.");
await Task.Delay(-1);