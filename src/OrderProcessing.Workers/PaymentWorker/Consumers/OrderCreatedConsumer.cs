using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Enums;
using OrderProcessing.Core.Messages;
using OrderProcessing.Infrastructure.Data;

namespace PaymentWorker.Consumers;

/// <summary>
/// Consumer que processa eventos de pedido criado.
/// Simula processamento de pagamento com 70% de aprovação.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly AppDbContext _context;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var orderId = context.Message.OrderId;
        _logger.LogInformation("Processando pagamento para pedido {OrderId}...", orderId);

        // Busca o pedido no banco
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            _logger.LogWarning("Pedido {OrderId} não encontrado no banco de dados. Descartando...", orderId);
            return;
        }

        // Atualiza status para processando
        order.PaymentStatus = EPaymentStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Simula processamento de pagamento (5 segundos)
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Simula aprovação do pagamento (70% de chance de aprovar)
        var approved = Random.Shared.Next(101) < 70;

        if (approved)
        {
            order.PaymentStatus = EPaymentStatus.Approved;
            order.OrderStatus = EOrderStatus.Processing;
            _logger.LogInformation("Pagamento APROVADO para o pedido {OrderId}", orderId);

            // Publica evento de pagamento aprovado para o próximo worker
            await context.Publish(new PaymentApproved { OrderId = orderId });
        }
        else
        {
            order.PaymentStatus = EPaymentStatus.Rejected;
            order.OrderStatus = EOrderStatus.Failed;
            _logger.LogWarning("Pagamento REJEITADO para o pedido {OrderId}", orderId);
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pagamento do pedido {OrderId} processado com sucesso.", orderId);
    }
}
