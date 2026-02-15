namespace OrderProcessing.Core.Messages;

/// <summary>
/// Evento publicado quando o pagamento é aprovado.
/// Consumido pelo InventoryWorker.
/// </summary>
public record PaymentApproved
{
    public Guid OrderId { get; init; }
}