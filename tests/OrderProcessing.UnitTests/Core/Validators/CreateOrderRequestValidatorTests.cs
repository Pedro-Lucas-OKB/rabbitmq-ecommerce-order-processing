using FluentAssertions;
using FluentAssertions.Equivalency;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Validators;
using Xunit.Abstractions;

namespace OrderProcessing.UnitTests.Core.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();
    private readonly ITestOutputHelper _output;

    public CreateOrderRequestValidatorTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithValidData_ShouldNotThrowException()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, 100.00m)
            }
        );
        
        _output.WriteLine($"[LOG] Teste iniciado com CustomerName: '{request.CustomerName}'");
        _output.WriteLine($"[LOG] Email: '{request.CustomerEmail}'");
        _output.WriteLine($"[LOG] Total de itens: {request.Items.Count}");
        
        // Act
        var result = _validator.Validate(request);
        
        _output.WriteLine($"[LOG] Resultado da validação: IsValid = {result.IsValid}");
        _output.WriteLine($"[LOG] Erros encontrados: {result.Errors.Count}");
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        
        _output.WriteLine("[LOG] ✅ Teste passou com sucesso!");
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithEmptyCustomerName_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, 100.00m)
            }
        );
        
        _output.WriteLine($"[LOG] Testando CustomerName vazio (tamanho: {request.CustomerName.Length})");
        
        // Act
        var result = _validator.Validate(request);
        
        _output.WriteLine($"[LOG] IsValid: {result.IsValid}");
        
        if (!result.IsValid)
        {
            _output.WriteLine("[LOG] Erros de validação:");
            foreach (var error in result.Errors)
            {
                _output.WriteLine($"  - {error.PropertyName}: {error.ErrorMessage}");
            }
        }
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateOrderRequest.CustomerName));
        
        _output.WriteLine("[LOG] ✅ Teste passou - Nome vazio corretamente rejeitado!");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("test")]
    [InlineData("testmail.com")]
    [InlineData("@test.com")]
    public void CreateOrderRequestValidator_WithInvalidCustomerEmail_ShouldBeInvalid(string email)
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: email,
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, 100.00m)
            }
        );
        
        _output.WriteLine($"[LOG] Testando email inválido: '{email}'");
        
        // Act
        var result = _validator.Validate(request);
        
        _output.WriteLine($"[LOG] Resultado: IsValid={result.IsValid}, Erros={result.Errors.Count}");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateOrderRequest.CustomerEmail));
        
        _output.WriteLine($"[LOG] ✅ Email '{email}' corretamente rejeitado");
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithNoOrderItems_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>()
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateOrderRequest.Items));       
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithCustomerNameTooLong_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: new string('A', 101), // 101 caracteres
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, 100.00m)
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateOrderRequest.CustomerName));
    }
    
    [Theory]
    [InlineData("test@email.com")]
    [InlineData("user@domain.com")]
    [InlineData("name.surname@company.org")]
    public void CreateOrderRequestValidator_WithValidCustomerEmail_ShouldBeValid(string email)
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: email,
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, 100.00m)
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithEmptyProductName_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("", "123456789", 1, 100.00m) // Nome vazio
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Items[0].ProductName");
    }
    
    [Fact]
    public void CreateOrderRequestValidator_WithEmptyProductSku_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "", 1, 100.00m) // SKU vazio
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Items[0].ProductSku");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateOrderRequestValidator_WithInvalidQuantity_ShouldBeInvalid(int quantity)
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", quantity, 100.00m)
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Items[0].Quantity");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-10.00)]
    public void CreateOrderRequestValidator_WithInvalidUnitPrice_ShouldBeInvalid(decimal unitPrice)
    {
        // Arrange
        var request = new CreateOrderRequest
        (
            CustomerName: "Test Name",
            CustomerEmail: "test@email.com",
            Items: new List<CreateOrderItemRequest>
            {
                new ("Test Product", "123456789", 1, unitPrice)
            }
        );
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Items[0].UnitPrice");
    }
}