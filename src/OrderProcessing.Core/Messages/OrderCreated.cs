namespace OrderProcessing.Core.Messages;

/// <summary>
/// Evento publicado quando um novo pedido é criado.
/// Consumido pelo PaymentWorker.
/// </summary>
public record OrderCreated
{
    public Guid OrderId { get; init; }
}