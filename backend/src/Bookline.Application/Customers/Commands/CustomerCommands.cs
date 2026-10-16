using Bookline.Application.Common.Models;
using Bookline.Application.Customers.DTOs;
using FluentValidation;
using MediatR;

namespace Bookline.Application.Customers.Commands;

// Queries
public record GetCustomersQuery(
    string? SearchQuery = null,
    bool IncludeArchived = false,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    string? Direction = "asc"
) : IRequest<PagedResult<CustomerDto>>;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

// Commands
public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<CustomerDto>;

public record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Request) : IRequest<CustomerDto>;

public record ArchiveCustomerCommand(Guid Id) : IRequest<bool>;

// Validators
public class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    private static readonly string[] AllowedSorts = { "name", "email", "created", "spent", "bookings" };

    public GetCustomersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SearchQuery)
            .MaximumLength(100)
            .WithMessage("Search query cannot exceed 100 characters.");

        RuleFor(x => x.Direction)
            .Must(d => string.IsNullOrEmpty(d) || d.Equals("asc", StringComparison.OrdinalIgnoreCase) || d.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be 'asc' or 'desc'.");

        RuleFor(x => x.Sort)
            .Must(s => string.IsNullOrEmpty(s) || AllowedSorts.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage($"Sort must be one of: {string.Join(", ", AllowedSorts)}.");
    }
}

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50);

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Valid email is required.");
    }
}
