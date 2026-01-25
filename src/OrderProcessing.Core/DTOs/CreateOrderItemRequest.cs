namespace OrderProcessing.Core.DTOs;

public record CreateOrderItemRequest(
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice
);
