using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Entities;
using OrderProcessing.Infrastructure.Data;

namespace IntegrationTests.Endpoints.Orders;

public class CreateOrderEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApiClient _client;
    
    public CreateOrderEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

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
        
        var createdOrder = await _client.ReadContentAsJsonAsync<Order>(response.Content);
        createdOrder.Should().NotBeNull();
        
        // Compara o resultado com o request, mas apenas para as propriedades que eles compartilham.
        createdOrder.Should().BeEquivalentTo(request, options => options
            .Including(x => x.CustomerName)
            .Including(x => x.CustomerEmail)
            .Including(x => x.Items));
        
        createdOrder.Id.Should().NotBe(Guid.Empty);
        createdOrder.Items.Count.Should().Be(2);
        createdOrder.TotalAmount.Should().Be(price1 + (2 * price2));

        // Obtenha o DbContext aqui para garantir que ele veja os dados da transação
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbOrder = await dbContext.Orders.
                AsNoTracking()
                .Include(order => order.Items)
                .FirstOrDefaultAsync(order => order.Id == createdOrder.Id);
        
        dbOrder.Should().NotBeNull();
        dbOrder.Should().BeEquivalentTo(createdOrder, options =>
        {
            return options
                .Excluding(x => x.CreatedAt)
                .Excluding(x => x.UpdatedAt)
                .For(x => x.Items)
                    .Exclude(i => i.OrderId)
                .For(x => x.Items)
                    .Exclude(i => i.Order);
        });       
    }
}