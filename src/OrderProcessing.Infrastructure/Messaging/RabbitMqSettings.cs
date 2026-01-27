namespace OrderProcessing.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public string ExchangeName { get; set; } = "order-created-exchange";
    public string QueueName { get; set; } = "payment-queue";
    public string RoutingKey { get; set; } = "order.created";
}