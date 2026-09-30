using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Customers.Commands;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime CreatedAtUtc
);

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone
) : IRequest<CustomerDto>;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public record GetCustomersQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<CustomerDto>>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone number is required.");
    }
}

public class CustomerCommandHandler :
    IRequestHandler<CreateCustomerCommand, CustomerDto>,
    IRequestHandler<GetCustomerByIdQuery, CustomerDto>,
    IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public CustomerCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerDto(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.Phone, customer.CreatedAtUtc);
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), request.Id);
        }

        return new CustomerDto(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.Phone, customer.CreatedAtUtc);
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery(request.Page, request.PageSize);

        var query = _dbContext.Customers.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .Select(c => new CustomerDto(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, totalCount, pagedQuery.Page, pagedQuery.PageSize);
    }
}
