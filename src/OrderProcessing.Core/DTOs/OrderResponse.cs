namespace OrderProcessing.Core.DTOs;

public record OrderItemResponse(
    Guid Id,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Status, // Retornando os enums como strings
    string PaymentStatus,
    string InventoryStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemResponse> Items
);
