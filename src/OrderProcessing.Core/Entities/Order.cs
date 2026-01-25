using OrderProcessing.Core.Enums;

namespace OrderProcessing.Core.Entities;

public class Order
{
    public Guid Id { get; init; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public EOrderStatus OrderStatus { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public EInventoryStatus InventoryStatus { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}