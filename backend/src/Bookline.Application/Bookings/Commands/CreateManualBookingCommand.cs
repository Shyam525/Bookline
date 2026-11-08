namespace Bookline.Application.Bookings.Commands;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record CreateManualBookingRequest(
    Guid StaffId,
    Guid ServiceId,
    DateTimeOffset StartUtc,
    BookingSource Source,
    string CustomerName,
    string? CustomerPhone = null,
    string? CustomerEmail = null,
    Guid? CustomerId = null,
    Guid? LocationId = null,
    string? CustomerNotes = null,
    string? InternalNotes = null,
    decimal? PriceOverride = null,
    bool AllowPast = false
);

public record CreateManualBookingCommand(CreateManualBookingRequest Request) : IRequest<BookingDto>;

public class CreateManualBookingCommandValidator : AbstractValidator<CreateManualBookingCommand>
{
    public CreateManualBookingCommandValidator()
    {
        RuleFor(x => x.Request.StaffId).NotEmpty().WithMessage("Staff specialist must be assigned.");
        RuleFor(x => x.Request.ServiceId).NotEmpty().WithMessage("Service must be selected.");
        RuleFor(x => x.Request.CustomerName).NotEmpty().WithMessage("Customer name is required.");
        RuleFor(x => x.Request.Source)
            .IsInEnum()
            .WithMessage("Appointment source must be one of: WalkIn, Phone, WhatsApp, Instagram, Manual, Online.");
        RuleFor(x => x.Request.PriceOverride)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.Request.PriceOverride.HasValue)
            .WithMessage("Price override cannot be negative.");
    }
}

public class CreateManualBookingCommandHandler : IRequestHandler<CreateManualBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAppointmentSchedulingService _schedulingService;

    public CreateManualBookingCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IAppointmentSchedulingService schedulingService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _schedulingService = schedulingService;
    }

    public async Task<BookingDto> Handle(CreateManualBookingCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // 1. Resolve Tenant Context
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            var fallbackStaff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == req.StaffId, cancellationToken);
            if (fallbackStaff != null)
            {
                tenantId = fallbackStaff.TenantId;
            }
            else
            {
                throw new ValidationException("TENANT_UNRESOLVED: Unable to identify the active business organization.");
            }
        }

        // 2. Fetch Service for duration and snapshot
        var service = await _context.Services
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == req.ServiceId && s.TenantId == tenantId, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), req.ServiceId);
        }

        var endUtc = req.StartUtc.AddMinutes(service.DurationMinutes);

        // 3. Shared Scheduling Conflict & Policy Engine Validation
        await _schedulingService.ValidateScheduleEligibilityAsync(
            tenantId: tenantId,
            staffId: req.StaffId,
            serviceId: req.ServiceId,
            startUtc: req.StartUtc,
            endUtc: endUtc,
            locationId: req.LocationId,
            excludeBookingId: null,
            allowPast: req.AllowPast,
            cancellationToken: cancellationToken);

        // 4. Resolve or Reuse Customer
        Customer customer;
        if (req.CustomerId.HasValue && req.CustomerId.Value != Guid.Empty)
        {
            var found = await _context.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == req.CustomerId.Value && c.TenantId == tenantId, cancellationToken);

            if (found == null)
            {
                throw new NotFoundException(nameof(Customer), req.CustomerId.Value);
            }
            customer = found;
            customer.TotalBookingsCount += 1;
            customer.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var phoneClean = !string.IsNullOrWhiteSpace(req.CustomerPhone) ? req.CustomerPhone.Trim() : null;
            var emailClean = !string.IsNullOrWhiteSpace(req.CustomerEmail) ? req.CustomerEmail.Trim().ToLowerInvariant() : null;

            Customer? existing = null;
            if (!string.IsNullOrEmpty(phoneClean) || !string.IsNullOrEmpty(emailClean))
            {
                existing = await _context.Customers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c =>
                        c.TenantId == tenantId && !c.IsArchived &&
                        ((phoneClean != null && c.Phone == phoneClean) ||
                         (emailClean != null && c.Email.ToLower() == emailClean)),
                        cancellationToken);
            }

            if (existing != null)
            {
                customer = existing;
                if (string.IsNullOrEmpty(customer.Phone) && !string.IsNullOrEmpty(phoneClean))
                {
                    customer.Phone = phoneClean;
                }
                if (string.IsNullOrEmpty(customer.Email) && !string.IsNullOrEmpty(emailClean))
                {
                    customer.Email = emailClean;
                }
                customer.TotalBookingsCount += 1;
                customer.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                var nameParts = req.CustomerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                    Phone = phoneClean ?? string.Empty,
                    Email = emailClean ?? $"{Guid.NewGuid().ToString("N")[..8]}@guest.bookline.local",
                    TotalBookingsCount = 1,
                    TotalSpentAmount = 0.00m,
                    IsArchived = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
            }
        }

        // 5. Retrieve Tenant for timezone & currency
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        var price = req.PriceOverride ?? service.Price;

        // 6. Instantiate Confirmed Manual Booking Entity
        var booking = Booking.CreateManual(
            tenantId: tenantId,
            staffId: req.StaffId,
            serviceId: req.ServiceId,
            customerId: customer.Id,
            startUtc: req.StartUtc,
            endUtc: endUtc,
            source: req.Source,
            serviceNameSnapshot: service.Name,
            priceSnapshot: price,
            currency: service.Currency ?? "INR",
            locationId: req.LocationId,
            customerNotes: req.CustomerNotes,
            internalNotes: req.InternalNotes,
            timeZoneId: tenant?.TimeZoneId ?? "Asia/Kolkata",
            createdBy: "Provider"
        );

        _context.Bookings.Add(booking);

        // 7. Atomic Audit Log & Outbox Event
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            Actor = "Provider",
            Action = "Appointment.CreatedManual",
            Target = booking.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                Source = req.Source.ToString(),
                StaffId = booking.StaffId,
                ServiceId = booking.ServiceId,
                CustomerId = booking.CustomerId,
                StartUtc = booking.StartUtc,
                EndUtc = booking.EndUtc,
                Price = price
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = tenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentConfirmed,
            Content = JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                TenantId = tenantId,
                CustomerId = customer.Id,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.Phone,
                StaffId = booking.StaffId,
                ServiceId = booking.ServiceId,
                ServiceName = service.Name,
                StartUtc = booking.StartUtc,
                EndUtc = booking.EndUtc,
                Source = req.Source.ToString()
            })
        });

        await _context.SaveChangesAsync(cancellationToken);

        var staff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == req.StaffId, cancellationToken);

        return new BookingDto(
            booking.Id,
            booking.TenantId,
            booking.StaffId,
            booking.ServiceId,
            booking.CustomerId,
            booking.StartUtc,
            booking.EndUtc,
            booking.Status,
            booking.CreatedAtUtc,
            booking.BookingReference,
            booking.RelatedOrderId,
            booking.RelatedOrderNumber,
            service.Name,
            staff?.Name ?? "Specialist",
            customer.FullName,
            customer.Phone,
            customer.Email,
            price,
            booking.Currency,
            booking.Source.ToString(),
            booking.CustomerNotes,
            booking.InternalNotes,
            booking.TimeZoneId,
            booking.LocationId
        );
    }
}

