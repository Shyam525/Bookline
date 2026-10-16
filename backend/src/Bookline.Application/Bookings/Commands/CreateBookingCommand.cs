namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Common;
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
    string? CustomerNotes = null,
    Guid? LocationId = null,
    Guid? ResourceId = null
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

        if (!service.IsActive || service.IsArchived || !service.IsPublished || !service.IsOnlineBookingEnabled)
        {
            throw new ValidationException("SERVICE_NOT_BOOKABLE: This service is unavailable for online booking.");
        }

        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : service.TenantId;

        // 2. Validate Staff Eligibility
        var staff = await _context.Staff
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && s.TenantId == tenantId, cancellationToken);

        if (staff == null || !staff.IsActive || staff.IsArchived)
        {
            throw new ValidationException("The selected staff member is invalid, inactive, or not part of this business.");
        }

        var hasAssignedStaff = await _context.StaffServices
            .IgnoreQueryFilters()
            .AnyAsync(ss => ss.ServiceId == service.Id, cancellationToken);

        if (hasAssignedStaff)
        {
            var isStaffAssigned = await _context.StaffServices
                .IgnoreQueryFilters()
                .AnyAsync(ss => ss.StaffId == request.StaffId && ss.ServiceId == service.Id, cancellationToken);

            if (!isStaffAssigned)
            {
                throw new ValidationException("The selected staff member is not eligible to perform this service.");
            }
        }

        // 3. Validate Location Applicability (if provided or restricted)
        if (request.LocationId.HasValue)
        {
            var location = await _context.Locations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == request.LocationId.Value && l.TenantId == tenantId && l.IsActive, cancellationToken);

            if (location == null)
            {
                throw new ValidationException("The selected location is invalid or inactive.");
            }

            var hasLocationRestrictions = await _context.ServiceLocations
                .IgnoreQueryFilters()
                .AnyAsync(sl => sl.ServiceId == service.Id, cancellationToken);

            if (hasLocationRestrictions)
            {
                var isLocationAssigned = await _context.ServiceLocations
                    .IgnoreQueryFilters()
                    .AnyAsync(sl => sl.ServiceId == service.Id && sl.LocationId == request.LocationId.Value && sl.IsAvailable, cancellationToken);

                if (!isLocationAssigned)
                {
                    throw new ValidationException("This service is not available at the selected location.");
                }
            }
        }

        // 4. Validate Resource Requirements & Potential Conflicts
        Guid? allocatedResourceId = request.ResourceId ?? service.RequiredResourceId;
        if (service.RequiresResource && allocatedResourceId == null)
        {
            var resQuery = _context.Resources
                .IgnoreQueryFilters()
                .Where(r => r.TenantId == tenantId && r.IsActive);

            if (request.LocationId.HasValue)
            {
                resQuery = resQuery.Where(r => r.LocationId == null || r.LocationId == request.LocationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(service.RequiredResourceType))
            {
                resQuery = resQuery.Where(r => r.Type.ToLower() == service.RequiredResourceType.ToLower());
            }

            var matchingRes = await resQuery.FirstOrDefaultAsync(cancellationToken);
            if (matchingRes != null)
            {
                allocatedResourceId = matchingRes.Id;
            }
        }

        if (allocatedResourceId.HasValue)
        {
            var resource = await _context.Resources
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == allocatedResourceId.Value && r.TenantId == tenantId && r.IsActive, cancellationToken);

            if (resource == null)
            {
                throw new ValidationException("The required resource is unavailable or belongs to another organization.");
            }

            var occupancyStart = request.StartUtc.AddMinutes(-service.BufferBeforeMinutes);
            var occupancyEnd = request.StartUtc.AddMinutes(service.DurationMinutes + service.BufferAfterMinutes);

            var resourceConflict = await _context.Bookings
                .IgnoreQueryFilters()
                .AnyAsync(b => b.TenantId == tenantId &&
                               b.ResourceId == allocatedResourceId.Value &&
                               (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                               b.StartUtc < occupancyEnd &&
                               b.EndUtc > occupancyStart,
                               cancellationToken);

            if (resourceConflict)
            {
                throw new ValidationException("The required resource is already reserved for this time window.");
            }
        }

        // 5. Verify Redis slot hold
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

        // 6. Instantiate Booking entity
        var booking = new Booking(
            tenantId,
            request.StaffId,
            request.ServiceId,
            customer.Id,
            request.StartUtc,
            endUtc);

        booking.LocationId = request.LocationId;
        booking.ResourceId = allocatedResourceId;
        booking.TotalPrice = service.Price;
        booking.Currency = service.Currency;
        booking.CustomerNotes = request.CustomerNotes;

        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant != null)
        {
            booking.DepositPolicy = tenant.DepositType;
            booking.DepositRequiredAmount = tenant.DepositType switch
            {
                Bookline.Domain.Enums.DepositType.Fixed => CurrencyRegistry.Round(tenant.DepositAmount, service.Currency),
                Bookline.Domain.Enums.DepositType.Percentage => CurrencyRegistry.Round(service.Price * (tenant.DepositAmount / 100m), service.Currency),
                _ => 0.00m
            };
        }

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
            booking.CreatedAtUtc,
            booking.BookingReference,
            booking.RelatedOrderId,
            booking.RelatedOrderNumber);
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
                    customer => (customer.Id == customerId || customer.UserId == customerId) && customer.TenantId == tenantId,
                    cancellationToken);

            if (existingCustomer != null)
            {
                return existingCustomer;
            }

            // Check if customerId is an AppUser platform user
            var appUser = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken);

            if (appUser != null)
            {
                var newCustomer = new Customer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = appUser.Id,
                    FirstName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim().Split(' ', 2)[0] : appUser.FirstName,
                    LastName = !string.IsNullOrWhiteSpace(request.CustomerName) && request.CustomerName.Trim().Split(' ', 2).Length > 1 ? request.CustomerName.Trim().Split(' ', 2)[1] : appUser.LastName,
                    Email = !string.IsNullOrWhiteSpace(request.CustomerEmail) ? request.CustomerEmail.Trim() : appUser.Email,
                    Phone = request.CustomerPhone?.Trim() ?? appUser.Phone ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Customers.Add(newCustomer);
                return newCustomer;
            }

            throw new NotFoundException(nameof(Customer), customerId);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new ValidationException("Customer name and email are required.");
        }

        var email = request.CustomerEmail.Trim();
        var customerByEmail = await _context.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                candidate => candidate.TenantId == tenantId && candidate.Email.ToLower() == email.ToLower(),
                cancellationToken);

        if (customerByEmail != null)
        {
            return customerByEmail;
        }

        // Check if an AppUser already exists with this email to link UserId
        var existingUserByEmail = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        var nameParts = request.CustomerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var createdCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = existingUserByEmail?.Id,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Email = email,
            Phone = request.CustomerPhone?.Trim() ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Customers.Add(createdCustomer);
        return createdCustomer;
    }

    private static bool IsExclusionViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            var innerType = inner.GetType();
            if (innerType.Name == "PostgresException")
            {
                var sqlStateProp = innerType.GetProperty("SqlState");
                var constraintProp = innerType.GetProperty("ConstraintName");

                var sqlState = sqlStateProp?.GetValue(inner)?.ToString();
                var constraint = constraintProp?.GetValue(inner)?.ToString();

                if (sqlState == "23P01" || constraint == "no_overlap" || constraint == "no_resource_overlap")
                {
                    return true;
                }
            }

            if (inner.Message.Contains("23P01") || inner.Message.Contains("no_overlap") || inner.Message.Contains("no_resource_overlap"))
            {
                return true;
            }

            inner = inner.InnerException;
        }

        return false;
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
