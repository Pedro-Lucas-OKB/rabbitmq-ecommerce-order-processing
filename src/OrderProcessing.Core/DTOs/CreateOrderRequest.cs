namespace OrderProcessing.Core.DTOs;

public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    List<CreateOrderItemRequest> Items
);
