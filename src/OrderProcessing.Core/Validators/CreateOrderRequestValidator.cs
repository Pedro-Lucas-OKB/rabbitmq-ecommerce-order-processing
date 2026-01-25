using FluentValidation;
using OrderProcessing.Core.DTOs;

namespace OrderProcessing.Core.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(order => order.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");
        
        RuleFor(order => order.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("Customer email is not valid.");
        
        RuleFor(order => order.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");
    }
}