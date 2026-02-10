using System.Net;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Infrastructure.Data;

namespace IntegrationTests.Endpoints.Orders;

/// <summary>
/// Testes de integracao para o endpoint POST /api/orders.
/// Testa o fluxo completo: HTTP Request -> API -> Banco de Dados.
/// </summary>
public class CreateOrderEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApiClient _client;
    
    public CreateOrderEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

    #region Testes de Sucesso

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturn201CreatedAndPersistOrder()
    {
        // Arrange
        var price1 = 100.00m;
        var price2 = 200.00m;
        var request = new CreateOrderRequest(
            CustomerName: "Test Name",
            CustomerEmail: "email@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Test Product", "123456789", 1, price1),
                new("Test Product 2", "987654321", 2, price2)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        createdOrder.Should().NotBeNull();
        createdOrder!.Id.Should().NotBe(Guid.Empty);
        createdOrder.CustomerName.Should().Be(request.CustomerName);
        createdOrder.CustomerEmail.Should().Be(request.CustomerEmail);
        createdOrder.Items.Count.Should().Be(2);
        createdOrder.TotalAmount.Should().Be(price1 + (2 * price2));
        createdOrder.Status.Should().Be("Pending");
        createdOrder.PaymentStatus.Should().Be("Pending");
        createdOrder.InventoryStatus.Should().Be("Pending");

        // Verifica persistencia no banco de dados
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbOrder = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == createdOrder.Id);
        
        dbOrder.Should().NotBeNull();
        dbOrder!.CustomerName.Should().Be(request.CustomerName);
        dbOrder.CustomerEmail.Should().Be(request.CustomerEmail);
        dbOrder.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WithSingleItem_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Single Item Customer",
            CustomerEmail: "single@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Single Product", "SKU001", 3, 50.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        createdOrder.Should().NotBeNull();
        createdOrder!.Items.Should().HaveCount(1);
        createdOrder.TotalAmount.Should().Be(150.00m); // 3 * 50.00
    }

    [Fact]
    public async Task CreateOrder_WithLargeQuantity_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Large Order Customer",
            CustomerEmail: "large@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Bulk Product", "BULK001", 1000, 1.50m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdOrder = await _client.ReadContentAsJsonAsync<OrderResponse>(response.Content);
        createdOrder.Should().NotBeNull();
        createdOrder!.TotalAmount.Should().Be(1500.00m); // 1000 * 1.50
    }

    #endregion

    #region Testes de Validacao - CustomerName

    [Fact]
    public async Task CreateOrder_WithEmptyCustomerName_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithCustomerNameExceeding100Chars_ShouldReturn400BadRequest()
    {
        // Arrange
        var longName = new string('A', 101);
        var request = new CreateOrderRequest(
            CustomerName: longName,
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Testes de Validacao - CustomerEmail

    [Fact]
    public async Task CreateOrder_WithEmptyCustomerEmail_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@nodomain.com")]
    [InlineData("nodomain@")]
    public async Task CreateOrder_WithInvalidEmail_ShouldReturn400BadRequest(string invalidEmail)
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: invalidEmail,
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Testes de Validacao - Items

    [Fact]
    public async Task CreateOrder_WithEmptyItemsList_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>());
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithItemQuantityZero_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 0, 10.00m) // Quantity = 0
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithNegativeQuantity_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", -1, 10.00m) // Negative quantity
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithZeroUnitPrice_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, 0.00m) // UnitPrice = 0
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithNegativeUnitPrice_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "SKU001", 1, -10.00m) // Negative price
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyProductName_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("", "SKU001", 1, 10.00m) // Empty product name
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyProductSku_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", "", 1, 10.00m) // Empty SKU
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithProductNameExceeding200Chars_ShouldReturn400BadRequest()
    {
        // Arrange
        var longProductName = new string('P', 201);
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new(longProductName, "SKU001", 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithProductSkuExceeding50Chars_ShouldReturn400BadRequest()
    {
        // Arrange
        var longSku = new string('S', 51);
        var request = new CreateOrderRequest(
            CustomerName: "Valid Name",
            CustomerEmail: "valid@test.com",
            Items: new List<CreateOrderItemRequest>
            {
                new("Product", longSku, 1, 10.00m)
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Testes de Validacao - Multiplos Erros

    [Fact]
    public async Task CreateOrder_WithMultipleValidationErrors_ShouldReturn400BadRequest()
    {
        // Arrange - Request com varios erros
        var request = new CreateOrderRequest(
            CustomerName: "", // Erro 1: nome vazio
            CustomerEmail: "invalid-email", // Erro 2: email invalido
            Items: new List<CreateOrderItemRequest>
            {
                new("", "SKU001", 0, -10.00m) // Erros 3, 4, 5: nome vazio, quantity 0, price negativo
            });
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
