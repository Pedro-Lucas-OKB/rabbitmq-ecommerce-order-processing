using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Messages;
using OrderProcessing.Infrastructure.Data;

namespace NotificationWorker.Consumers;

/// <summary>
/// Consumer que processa eventos de estoque reservado.
/// Simula envio de notificação por e-mail ao cliente.
/// </summary>
public class InventoryReservedConsumer : IConsumer<InventoryReserved>
{
    private readonly ILogger<InventoryReservedConsumer> _logger;
    private readonly AppDbContext _context;

    public InventoryReservedConsumer(
        ILogger<InventoryReservedConsumer> logger,
        AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var orderId = context.Message.OrderId;
        var customerEmail = context.Message.CustomerEmail;
        
        _logger.LogInformation("Processando notificação para pedido {OrderId}...", orderId);

        // Busca o pedido no banco para obter detalhes adicionais
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            _logger.LogWarning("Pedido {OrderId} não encontrado no banco de dados. Descartando...", orderId);
            return;
        }

        // Simulando envio de e-mail (2 segundos)
        _logger.LogInformation("Enviando notificação para o email {CustomerEmail} referente ao pedido {OrderId}...", 
            customerEmail, orderId);
        await Task.Delay(TimeSpan.FromSeconds(2));

        _logger.LogInformation(
            "Notificação via e-mail ENVIADA para o cliente {CustomerEmail}. Pedido {OrderId} - Status: {Status}", 
            customerEmail, orderId, order.OrderStatus.ToString());

        _logger.LogInformation("Notificação do pedido {OrderId} processada com sucesso.", orderId);
    }
}
