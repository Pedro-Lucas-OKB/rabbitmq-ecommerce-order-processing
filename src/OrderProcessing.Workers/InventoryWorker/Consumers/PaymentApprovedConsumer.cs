using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Enums;
using OrderProcessing.Core.Messages;
using OrderProcessing.Infrastructure.Data;

namespace InventoryWorker.Consumers;

/// <summary>
/// Consumer que processa eventos de pagamento aprovado.
/// Simula processamento de estoque com 90% de reserva.
/// </summary>
public class PaymentApprovedConsumer : IConsumer<PaymentApproved>
{
    private readonly ILogger<PaymentApprovedConsumer> _logger;
    private readonly AppDbContext _context;

    public PaymentApprovedConsumer(
        ILogger<PaymentApprovedConsumer> logger,
        AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<PaymentApproved> context)
    {
        var orderId = context.Message.OrderId;
        _logger.LogInformation("Processando estoque para pedido {OrderId}...", orderId);

        // Busca o pedido no banco
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            _logger.LogWarning("Pedido {OrderId} não encontrado no banco de dados. Descartando...", orderId);
            return;
        }

        // Simula processamento de estoque (3 segundos)
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Simula reserva do estoque (90% de chance de reservar)
        var reserved = Random.Shared.Next(101) < 90;

        if (reserved)
        {
            order.InventoryStatus = EInventoryStatus.Reserved;
            order.OrderStatus = EOrderStatus.Completed;
            _logger.LogInformation("Estoque RESERVADO para o pedido {OrderId}", orderId);

            // Publica evento de estoque reservado para o próximo worker
            await context.Publish(new InventoryReserved 
            { 
                OrderId = orderId,
                CustomerEmail = order.CustomerEmail
            });
        }
        else
        {
            order.InventoryStatus = EInventoryStatus.OutOfStock;
            order.OrderStatus = EOrderStatus.Failed;
            _logger.LogWarning("SEM ESTOQUE para o pedido {OrderId}", orderId);
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Estoque do pedido {OrderId} processado com sucesso.", orderId);
    }
}
