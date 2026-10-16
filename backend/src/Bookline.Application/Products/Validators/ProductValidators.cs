using Bookline.Application.Products.Commands;
using FluentValidation;

namespace Bookline.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(0.00m).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(1000000m).WithMessage("Price exceeds maximum permitted limit.");

        RuleFor(x => x.Request.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.Request.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low stock threshold cannot be negative.");

        When(x => !string.IsNullOrWhiteSpace(x.Request.Currency), () =>
        {
            RuleFor(x => x.Request.Currency)
                .Length(3).WithMessage("Currency must be a 3-letter ISO code.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Request.Sku), () =>
        {
            RuleFor(x => x.Request.Sku)
                .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.");
        });
    }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(0.00m).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(1000000m).WithMessage("Price exceeds maximum permitted limit.");

        RuleFor(x => x.Request.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.Request.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low stock threshold cannot be negative.");

        When(x => !string.IsNullOrWhiteSpace(x.Request.Currency), () =>
        {
            RuleFor(x => x.Request.Currency)
                .Length(3).WithMessage("Currency must be a 3-letter ISO code.");
        });
    }
}

public class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Request.NewStockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("New stock quantity cannot be negative.");

        When(x => !string.IsNullOrWhiteSpace(x.Request.Reason), () =>
        {
            RuleFor(x => x.Request.Reason)
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
        });
    }
}
