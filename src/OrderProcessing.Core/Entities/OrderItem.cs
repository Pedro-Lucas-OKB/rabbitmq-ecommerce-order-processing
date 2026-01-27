using System.Text.Json.Serialization;

namespace OrderProcessing.Core.Entities;

public class OrderItem
{
    public Guid Id { get; init; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Guid OrderId { get; init; }
    [JsonIgnore]
    public Order? Order { get; set; }
}