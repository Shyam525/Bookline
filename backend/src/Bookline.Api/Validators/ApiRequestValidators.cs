using Bookline.Api.Controllers;
using FluentValidation;

namespace Bookline.Api.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Provider tenant ID is required.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one product.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemRequestValidator());
    }
}

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 1000).WithMessage("Quantity must be between 1 and 1000.");
    }
}

public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequest>
{
    public SubmitReviewRequestValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Provider tenant ID is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required.")
            .MinimumLength(5).WithMessage("Comment must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");

        When(x => !string.IsNullOrWhiteSpace(x.Title), () =>
        {
            RuleFor(x => x.Title)
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");
        });
    }
}
