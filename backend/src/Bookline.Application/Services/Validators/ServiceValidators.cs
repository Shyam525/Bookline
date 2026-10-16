using Bookline.Application.Services.Commands;
using FluentValidation;

namespace Bookline.Application.Services.Validators;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MinimumLength(2).WithMessage("Service name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Service name cannot exceed 150 characters.");

        RuleFor(x => x.Request.DurationMinutes)
            .InclusiveBetween(5, 1440)
            .WithMessage("Duration must be between 5 and 1440 minutes (24 hours).");

        RuleFor(x => x.Request.BufferBeforeMinutes)
            .InclusiveBetween(0, 240)
            .WithMessage("Buffer before must be between 0 and 240 minutes.");

        RuleFor(x => x.Request.BufferAfterMinutes)
            .InclusiveBetween(0, 240)
            .WithMessage("Buffer after must be between 0 and 240 minutes.");

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(0.00m)
            .WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(1000000m)
            .WithMessage("Price exceeds maximum permitted limit.");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        When(x => !string.IsNullOrWhiteSpace(x.Request.ColorHex), () =>
        {
            RuleFor(x => x.Request.ColorHex)
                .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
                .WithMessage("Color must be a valid hex color code (e.g. #E8546A).");
        });
    }
}

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Service ID is required.");

        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MinimumLength(2).WithMessage("Service name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Service name cannot exceed 150 characters.");

        RuleFor(x => x.Request.DurationMinutes)
            .InclusiveBetween(5, 1440)
            .WithMessage("Duration must be between 5 and 1440 minutes (24 hours).");

        RuleFor(x => x.Request.BufferBeforeMinutes)
            .InclusiveBetween(0, 240)
            .WithMessage("Buffer before must be between 0 and 240 minutes.");

        RuleFor(x => x.Request.BufferAfterMinutes)
            .InclusiveBetween(0, 240)
            .WithMessage("Buffer after must be between 0 and 240 minutes.");

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(0.00m)
            .WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(1000000m)
            .WithMessage("Price exceeds maximum permitted limit.");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");
    }
}

public class CreateServiceCategoryCommandValidator : AbstractValidator<CreateServiceCategoryCommand>
{
    public CreateServiceCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");
    }
}

public class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Resource name is required.")
            .MinimumLength(2).WithMessage("Resource name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Resource name cannot exceed 100 characters.");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Resource type is required.")
            .MaximumLength(50).WithMessage("Resource type cannot exceed 50 characters.");

        RuleFor(x => x.Request.Capacity)
            .InclusiveBetween(1, 1000)
            .WithMessage("Capacity must be between 1 and 1000.");
    }
}

public class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Resource ID is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Resource name is required.")
            .MinimumLength(2).WithMessage("Resource name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Resource name cannot exceed 100 characters.");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Resource type is required.")
            .MaximumLength(50).WithMessage("Resource type cannot exceed 50 characters.");

        RuleFor(x => x.Request.Capacity)
            .InclusiveBetween(1, 1000)
            .WithMessage("Capacity must be between 1 and 1000.");
    }
}
