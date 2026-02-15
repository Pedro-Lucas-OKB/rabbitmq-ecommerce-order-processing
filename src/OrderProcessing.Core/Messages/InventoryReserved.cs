namespace OrderProcessing.Core.Messages;

/// <summary>
/// Evento publicado quando o estoque é reservado.
/// Consumido pelo NotificationWorker.
/// </summary>
public record InventoryReserved
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;  // Necessário para enviar notificação
}