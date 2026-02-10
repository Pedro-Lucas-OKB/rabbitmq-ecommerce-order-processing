using System.Net;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Enums;
using OrderProcessing.Infrastructure.Data;

namespace IntegrationTests.Endpoints.Orders;

/// <summary>
/// Testes de integracao para os endpoints GET /api/orders e GET /api/orders/{id}.
/// </summary>
public class GetOrderEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApiClient _client;
    
    public GetOrderEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

    #region GET /api/orders/{id}

    [Fact]
    public async Task GetOrderById_WithExistingOrder_ShouldReturn200Ok()
    {
        // Arrange - Cria um pedido via API primeiro
        var createRequest = new CreateOrderRequest(
            CustomerName: "Get Test Customer",
            CustomerEmail: "gettest@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Test Product", "SKU-GET-001", 2, 25.00m)
            });
        
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(createResponse.Content);
        
        // Act
        var response = await _client.HttpClient.GetAsync($"/api/orders/{createdOrder!.Id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var order = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        order.Should().NotBeNull();
        order!.Id.Should().Be(createdOrder.Id);
        order.CustomerName.Should().Be(createRequest.CustomerName);
        order.CustomerEmail.Should().Be(createRequest.CustomerEmail);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(50.00m); // 2 * 25.00
    }

    [Fact]
    public async Task GetOrderById_WithNonExistingOrder_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        
        // Act
        var response = await _client.HttpClient.GetAsync($"/api/orders/{nonExistingId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrderWithAllItems()
    {
        // Arrange - Cria um pedido com multiplos itens
        var createRequest = new CreateOrderRequest(
            CustomerName: "Multi Item Customer",
            CustomerEmail: "multi@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product A", "SKU-A", 1, 10.00m),
                new("Product B", "SKU-B", 2, 20.00m),
                new("Product C", "SKU-C", 3, 30.00m)
            });
        
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(createResponse.Content);
        
        // Act
        var response = await _client.HttpClient.GetAsync($"/api/orders/{createdOrder!.Id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var order = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        order.Should().NotBeNull();
        order!.Items.Should().HaveCount(3);
        order.TotalAmount.Should().Be(10.00m + 40.00m + 90.00m); // 10 + (2*20) + (3*30)
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnCorrectStatuses()
    {
        // Arrange
        var createRequest = new CreateOrderRequest(
            CustomerName: "Status Test Customer",
            CustomerEmail: "status@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Status Product", "SKU-STATUS", 1, 100.00m)
            });
        
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(createResponse.Content);
        
        // Act
        var response = await _client.HttpClient.GetAsync($"/api/orders/{createdOrder!.Id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var order = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        order.Should().NotBeNull();
        order!.Status.Should().Be("Pending");
        order.PaymentStatus.Should().Be("Pending");
        order.InventoryStatus.Should().Be("Pending");
    }

    #endregion

    #region GET /api/orders

    [Fact]
    public async Task GetAllOrders_ShouldReturnListOfOrders()
    {
        // Arrange - Cria alguns pedidos
        var request1 = new CreateOrderRequest(
            CustomerName: "List Customer 1",
            CustomerEmail: "list1@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product 1", "SKU-LIST-001", 1, 10.00m)
            });
        
        var request2 = new CreateOrderRequest(
            CustomerName: "List Customer 2",
            CustomerEmail: "list2@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product 2", "SKU-LIST-002", 1, 20.00m)
            });
        
        await _client.PostAsJsonAsync("/api/orders", request1);
        await _client.PostAsJsonAsync("/api/orders", request2);
        
        // Act
        var response = await _client.HttpClient.GetAsync("/api/orders");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var orders = await _client.ReadContentAsJsonAsync<List<OrderResponse>>(response.Content);
        orders.Should().NotBeNull();
        orders!.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetAllOrders_ShouldReturnOrdersWithItems()
    {
        // Arrange - Cria um pedido com itens
        var request = new CreateOrderRequest(
            CustomerName: "Items Test Customer",
            CustomerEmail: "items@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Item Product 1", "SKU-ITEM-001", 1, 15.00m),
                new("Item Product 2", "SKU-ITEM-002", 2, 25.00m)
            });
        
        var createResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(createResponse.Content);
        
        // Act
        var response = await _client.HttpClient.GetAsync("/api/orders");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var orders = await _client.ReadContentAsJsonAsync<List<OrderResponse>>(response.Content);
        orders.Should().NotBeNull();
        
        var matchingOrder = orders!.FirstOrDefault(o => o.Id == createdOrder!.Id);
        matchingOrder.Should().NotBeNull();
        matchingOrder!.Items.Should().HaveCount(2);
    }

    #endregion
}
