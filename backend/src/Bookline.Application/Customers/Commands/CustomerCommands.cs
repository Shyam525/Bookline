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
    int PageSize = 20
) : IRequest<PagedResult<CustomerDto>>;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

// Commands
public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<CustomerDto>;

public record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Request) : IRequest<CustomerDto>;

public record ArchiveCustomerCommand(Guid Id) : IRequest<bool>;

// Validators
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
