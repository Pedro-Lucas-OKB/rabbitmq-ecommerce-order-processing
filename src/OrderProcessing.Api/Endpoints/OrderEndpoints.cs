using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Enums;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Messaging;

namespace OrderProcessing.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
    }

    private static async Task<IResult> CreateAsync(
        CreateOrderRequest request,
        IValidator<CreateOrderRequest> validator,
        AppDbContext context,
        IRabbitMqPublisher publisher,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("OrderEndpoints");
        logger.LogInformation("Criando pedido para o cliente {CustomerName}", request.CustomerName);
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            OrderStatus = EOrderStatus.Pending,
            PaymentStatus = EPaymentStatus.Pending,
            InventoryStatus = EInventoryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice),
            Items = request.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            }).ToList()
        };
        
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        await publisher.PublishOrderCreatedAsync(order);
        
        logger.LogInformation("Pedido {OrderId} criado com sucesso.", order.Id);
        
        var response = MapToResponse(order);
        
        return Results.Created($"/api/orders/{order.Id}", response);
    }

    private static async Task<IResult> GetAllAsync(AppDbContext context)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ToListAsync();
        
        var response = orders.Select(MapToResponse).ToList();
        
        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext context)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id);
        
        if (order is null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(MapToResponse(order));
    }
    
    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.TotalAmount,
            order.OrderStatus.ToString(),
            order.PaymentStatus.ToString(),
            order.InventoryStatus.ToString(),
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.ProductName,
                item.ProductSku,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice
            )).ToList()
        );
    }
}