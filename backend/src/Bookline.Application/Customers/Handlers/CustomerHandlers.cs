using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
using Bookline.Application.Customers.DTOs;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Customers.Handlers;

public class CustomerCommandHandler :
    IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>,
    IRequestHandler<GetCustomerByIdQuery, CustomerDto>,
    IRequestHandler<CreateCustomerCommand, CustomerDto>,
    IRequestHandler<UpdateCustomerCommand, CustomerDto>,
    IRequestHandler<ArchiveCustomerCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CustomerCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var search = request.SearchQuery.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(search) ||
                c.LastName.ToLower().Contains(search) ||
                c.Email.ToLower().Contains(search) ||
                c.Phone.ToLower().Contains(search));
        }

        var pagedQuery = new PagedQuery(request.Page, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<CustomerDto>(dtos, totalCount, pagedQuery.Page, pagedQuery.PageSize);
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null) throw new NotFoundException(nameof(Customer), request.Id);

        return MapToDto(customer);
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new ValidationException("Email address is required.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName?.Trim() ?? string.Empty,
            Email = req.Email.Trim(),
            Phone = req.Phone?.Trim() ?? string.Empty,
            Notes = req.Notes,
            AvatarUrl = req.AvatarUrl,
            TotalBookingsCount = 0,
            TotalSpentAmount = 0.00m,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer == null) throw new NotFoundException(nameof(Customer), command.Id);

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new ValidationException("Email address is required.");

        customer.FirstName = req.FirstName.Trim();
        customer.LastName = req.LastName?.Trim() ?? string.Empty;
        customer.Email = req.Email.Trim();
        customer.Phone = req.Phone?.Trim() ?? string.Empty;
        customer.Notes = req.Notes;
        customer.AvatarUrl = req.AvatarUrl;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<bool> Handle(ArchiveCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (customer == null) throw new NotFoundException(nameof(Customer), command.Id);

        customer.IsArchived = true;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.TenantId,
            customer.FirstName,
            customer.LastName,
            customer.FullName,
            customer.Email,
            customer.Phone,
            customer.Notes,
            customer.AvatarUrl,
            customer.TotalBookingsCount,
            customer.TotalSpentAmount,
            customer.IsArchived,
            customer.CreatedAtUtc
        );
    }
}
