namespace OrderProcessing.Core.Entities;

public class OrderItem
{
    public ulong Id { get; init; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    
    public ulong OrderId { get; init; }
    public Order? Order { get; set; }
}