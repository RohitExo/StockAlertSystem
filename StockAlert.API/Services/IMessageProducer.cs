namespace StockAlert.API.Services
{
    public interface IMessageProducer
    {
        public Task SendingMessage<T>(T message);
    }
}
