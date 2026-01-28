namespace OrderProcessing.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public string ExchangeName { get; set; } = "order-created-exchange";
    public string PaymentQueueName { get; set; } = "payment-queue";
    public string InventoryQueueName { get; set; } = "inventory-queue";
    public string OrderCreatedRoutingKey { get; set; } = "order.created";
    public string PaymentApprovedRoutingKey { get; set; } = "payment.approved";
}