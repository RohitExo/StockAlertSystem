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
        long retryCount = GetRetryCount(ea.BasicProperties.Headers);
        if (retryCount < 3)
        {
            Console.WriteLine($"[Retry {retryCount + 1}/3] Delaying 5s: {ex.Message}");
            // Nack with requeue: false triggers the Dead Letter Exchange (pointing to Retry Queue)
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        else
        {
            Console.WriteLine($"[FATAL] Max retries reached. Routing to Permanent DLQ.");
            // Manually move to final DLQ so it doesn't loop forever
            await _channel.BasicPublishAsync(exchange: "stock-alert-dlx", routingKey: "permanent-dead", body: body);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
    }
};

await _channel.BasicConsumeAsync(queue: "stock-alert", autoAck: false, consumer: consumer);
Console.WriteLine("Consuming Stock Alerts... Press enter to exit.");
await Task.Delay(-1);

static long GetRetryCount(IDictionary<string, object?>? headers)
{
    if (headers != null && headers.ContainsKey("x-death"))
    {
        var deathList = headers["x-death"] as List<object>;
        if (deathList?.Count > 0)
        {
            var lastDeath = deathList[0] as IDictionary<string, object>;
            return (long)(lastDeath?["count"] ?? 0);
        }
    }
    return 0;
}