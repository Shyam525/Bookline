namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;


public record CreateBookingCommand(
    Guid StaffId,
    Guid ServiceId,
    Guid? CustomerId,
    DateTimeOffset StartUtc,
    Guid HoldId,
    string? CustomerName = null,
    string? CustomerEmail = null,
    string? CustomerPhone = null,
    string? CustomerNotes = null
) : IRequest<BookingDto>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty().When(x => x.CustomerId.HasValue);
        RuleFor(x => x.CustomerName).NotEmpty().When(x => !x.CustomerId.HasValue);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().When(x => !x.CustomerId.HasValue);
        RuleFor(x => x.HoldId).NotEmpty();
        RuleFor(x => x.StartUtc).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }
}

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ISlotHoldService _slotHoldService;
    private readonly ISlotEngine _slotEngine;
    private readonly IEmailSender? _emailSender;
    private readonly IBookingActionTokenService? _tokenService;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ISlotHoldService slotHoldService,
        ISlotEngine slotEngine,
        IEmailSender? emailSender = null,
        IBookingActionTokenService? tokenService = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _slotHoldService = slotHoldService;
        _slotEngine = slotEngine;
        _emailSender = emailSender;
        _tokenService = tokenService;
    }

    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // 1. Retrieve Service for duration & tenant identification
        var service = await _context.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
        if (service == null)
        {
            throw new NotFoundException(nameof(Service), request.ServiceId);
        }

        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : service.TenantId;

        // 2. Verify Redis slot hold
        var isValidHold = await _slotHoldService.ValidateHoldAsync(
            tenantId,
            request.StaffId,
            request.StartUtc,
            request.HoldId,
            cancellationToken);

        if (!isValidHold)
        {
            throw new ValidationException("Slot hold is invalid or has expired.");
        }

        var customer = await ResolveCustomerAsync(request, tenantId, cancellationToken);

        var endUtc = request.StartUtc.AddMinutes(service.DurationMinutes);

        // 3. Instantiate Booking entity
        var booking = new Booking(
            tenantId,
            request.StaffId,
            request.ServiceId,
            customer.Id,
            request.StartUtc,
            endUtc);

        booking.CustomerNotes = request.CustomerNotes;

        _context.Bookings.Add(booking);

        // Section 90 & 91: Atomic Audit & Outbox Event
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            Actor = request.CustomerId?.ToString() ?? request.CustomerEmail ?? "Customer",
            Action = "Booking.Created",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                ServiceId = booking.ServiceId,
                StaffId = booking.StaffId,
                StartUtc = booking.StartUtc,
                EndUtc = booking.EndUtc
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = tenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentCreated,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                TenantId = tenantId,
                CustomerId = booking.CustomerId,
                CustomerEmail = request.CustomerEmail ?? customer.Email,
                CustomerPhone = request.CustomerPhone ?? customer.Phone,
                StaffId = booking.StaffId,
                ServiceId = booking.ServiceId,
                ServiceName = service.Name,
                StartUtc = booking.StartUtc,
                EndUtc = booking.EndUtc
            })
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (IsExclusionViolation(ex))
            {
                var nearestSlots = await FindNearestFreeSlotsAsync(tenantId, request.StaffId, service, request.StartUtc, cancellationToken);
                throw new BookingConflictException(nearestSlots);
            }
            throw;
        }

        // 4. Release Redis hold after successful persistence
        await _slotHoldService.ReleaseHoldAsync(tenantId, request.StaffId, request.StartUtc, request.HoldId, cancellationToken);

        // 5. Send confirmation email (best-effort)
        if (_emailSender != null && _tokenService != null)
        {
            try
            {
                var staff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);
                var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                if (staff != null && customer != null && tenant != null)
                {
                    var rescheduleToken = _tokenService.GenerateToken(booking.Id, "reschedule", TimeSpan.FromDays(7));
                    var cancelToken = _tokenService.GenerateToken(booking.Id, "cancel", TimeSpan.FromDays(7));

                    await _emailSender.SendBookingConfirmationEmailAsync(
                        booking, service, staff, customer, tenant, rescheduleToken, cancelToken, cancellationToken);
                }
            }
            catch
            {
                // Non-blocking email send failure
            }
        }

        return new BookingDto(
            booking.Id,
            booking.TenantId,
            booking.StaffId,
            booking.ServiceId,
            booking.CustomerId,
            booking.StartUtc,
            booking.EndUtc,
            booking.Status,
            booking.CreatedAtUtc);
    }

    private async Task<Customer> ResolveCustomerAsync(
        CreateBookingCommand request,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId is Guid customerId)
        {
            var existingCustomer = await _context.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    customer => customer.Id == customerId && customer.TenantId == tenantId,
                    cancellationToken);

            return existingCustomer ?? throw new NotFoundException(nameof(Customer), customerId);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new ValidationException("Customer name and email are required.");
        }

        var email = request.CustomerEmail.Trim();
        var customer = await _context.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                candidate => candidate.TenantId == tenantId && candidate.Email.ToLower() == email.ToLower(),
                cancellationToken);

        if (customer != null)
        {
            return customer;
        }

        var nameParts = request.CustomerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Email = email,
            Phone = request.CustomerPhone?.Trim() ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        return customer;
    }

    private static bool IsExclusionViolation(DbUpdateException ex)
    {
        if (ex.InnerException == null) return false;

        var innerType = ex.InnerException.GetType();
        if (innerType.Name == "PostgresException")
        {
            var sqlStateProp = innerType.GetProperty("SqlState");
            var constraintProp = innerType.GetProperty("ConstraintName");

            var sqlState = sqlStateProp?.GetValue(ex.InnerException)?.ToString();
            var constraint = constraintProp?.GetValue(ex.InnerException)?.ToString();

            return sqlState == "23P01" || constraint == "no_overlap";
        }

        return ex.InnerException.Message.Contains("23P01") || ex.InnerException.Message.Contains("no_overlap");
    }


    private async Task<IReadOnlyList<Slot>> FindNearestFreeSlotsAsync(
        Guid tenantId,
        Guid staffId,
        Service service,
        DateTimeOffset requestedStartUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var staff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);
            if (staff == null) return Array.Empty<Slot>();

            var zone = NodaTime.DateTimeZoneProviders.Tzdb.GetZoneOrNull(staff.TimeZoneId) 
                ?? NodaTime.DateTimeZoneProviders.Tzdb["UTC"];


            var requestedInstant = NodaTime.Instant.FromDateTimeOffset(requestedStartUtc);
            var localDate = requestedInstant.InZone(zone).Date;

            var workingHours = await _context.WorkingHours
                .IgnoreQueryFilters()
                .Where(w => w.StaffId == staffId)
                .ToListAsync(cancellationToken);

            var existingBookings = await _context.Bookings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.StaffId == staffId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .ToListAsync(cancellationToken);

            var timeOffs = await _context.TimeOffs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.StaffId == staffId)
                .ToListAsync(cancellationToken);

            var dayHours = workingHours.Where(w => (int)w.DayOfWeek == (int)localDate.DayOfWeek).ToList();
            var windows = dayHours.Select(w => new WorkingWindow(
                NodaTime.LocalTime.FromTicksSinceMidnight(w.StartTime.Ticks),
                NodaTime.LocalTime.FromTicksSinceMidnight(w.EndTime.Ticks)
            )).ToList();

            var serviceInfo = new ServiceInfo(
                NodaTime.Duration.FromMinutes(service.DurationMinutes),
                NodaTime.Duration.FromMinutes(service.BufferMinutes));

            var schedule = new StaffSchedule(windows);

            var bookingIntervals = existingBookings.Select(b => new NodaTime.Interval(
                NodaTime.Instant.FromDateTimeOffset(b.StartUtc),
                NodaTime.Instant.FromDateTimeOffset(b.EndUtc)
            )).ToList();

            var timeOffIntervals = timeOffs.Select(t => new NodaTime.Interval(
                NodaTime.Instant.FromDateTimeOffset(t.StartUtc),
                NodaTime.Instant.FromDateTimeOffset(t.EndUtc)
            )).ToList();

            var slots = _slotEngine.Compute(
                serviceInfo,
                schedule,
                bookingIntervals,
                timeOffIntervals,
                localDate,
                zone,
                NodaTime.SystemClock.Instance.GetCurrentInstant(),
                NodaTime.Duration.FromMinutes(15),
                NodaTime.Duration.Zero);

            return slots
                .OrderBy(s => Math.Abs((s.Start - requestedInstant).TotalMinutes))
                .Take(3)
                .ToList();
        }
        catch
        {
            return Array.Empty<Slot>();
        }
    }
}
