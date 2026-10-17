using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Staff.Commands;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.Staff.Handlers;

public class StaffCommandHandler :
    IRequestHandler<GetAllStaffQuery, List<StaffDto>>,
    IRequestHandler<GetPagedStaffQuery, Bookline.Application.Common.Models.PagedResult<StaffDto>>,
    IRequestHandler<GetStaffByIdQuery, StaffDto>,
    IRequestHandler<CreateStaffCommand, StaffDto>,
    IRequestHandler<UpdateStaffCommand, StaffDto>,
    IRequestHandler<ArchiveStaffCommand, bool>,
    IRequestHandler<AssignStaffServicesCommand, StaffDto>,
    IRequestHandler<SetWorkingHoursCommand, IReadOnlyList<WorkingHourDto>>,
    IRequestHandler<CreateTimeOffCommand, TimeOffDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public StaffCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<StaffDto>> Handle(GetAllStaffQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Staff
            .AsNoTracking()
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        var staffList = await query.OrderBy(s => s.Name).ThenBy(s => s.Id).ToListAsync(cancellationToken);
        return staffList.Select(MapToDto).ToList();
    }

    public async Task<Bookline.Application.Common.Models.PagedResult<StaffDto>> Handle(GetPagedStaffQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Staff
            .AsNoTracking()
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search) || s.Email.ToLower().Contains(search) || (s.Title != null && s.Title.ToLower().Contains(search)));
        }

        var safePage = Math.Max(1, request.Page);
        var safePageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        bool isAsc = string.Equals(request.Direction, "asc", StringComparison.OrdinalIgnoreCase);
        query = (request.Sort?.ToLowerInvariant()) switch
        {
            "email" => isAsc ? query.OrderBy(s => s.Email).ThenBy(s => s.Id) : query.OrderByDescending(s => s.Email).ThenBy(s => s.Id),
            "title" => isAsc ? query.OrderBy(s => s.Title).ThenBy(s => s.Id) : query.OrderByDescending(s => s.Title).ThenBy(s => s.Id),
            _ => isAsc ? query.OrderBy(s => s.Name).ThenBy(s => s.Id) : query.OrderByDescending(s => s.Name).ThenBy(s => s.Id)
        };

        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new Bookline.Application.Common.Models.PagedResult<StaffDto>(dtos, totalCount, safePage, safePageSize);
    }

    public async Task<StaffDto> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .AsNoTracking()
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff == null) throw new NotFoundException(nameof(DomainStaff), request.Id);

        return MapToDto(staff);
    }

    public async Task<StaffDto> Handle(CreateStaffCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Staff name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new ValidationException("Email address is required.");

        var timeZoneId = string.IsNullOrWhiteSpace(req.TimeZoneId) ? "UTC" : req.TimeZoneId.Trim();
        if (NodaTime.DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) == null)
        {
            throw new ValidationException($"INVALID_TIMEZONE: '{timeZoneId}' is not a valid IANA timezone identifier.");
        }

        var staff = new DomainStaff
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = req.Name.Trim(),
            Email = req.Email.Trim(),
            Phone = req.Phone?.Trim(),
            Title = string.IsNullOrWhiteSpace(req.Title) ? "Staff Member" : req.Title.Trim(),
            Bio = req.Bio,
            AvatarUrl = req.AvatarUrl,
            TimeZoneId = timeZoneId,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (req.AssignedServiceIds != null && req.AssignedServiceIds.Any())
        {
            foreach (var serviceId in req.AssignedServiceIds.Distinct())
            {
                staff.StaffServices.Add(new StaffService
                {
                    StaffId = staff.Id,
                    ServiceId = serviceId,
                    TenantId = _tenantContext.TenantId
                });
            }
        }

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync(cancellationToken);

        var savedStaff = await _context.Staff
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .FirstAsync(s => s.Id == staff.Id, cancellationToken);

        return MapToDto(savedStaff);
    }

    public async Task<StaffDto> Handle(UpdateStaffCommand command, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (staff == null) throw new NotFoundException(nameof(DomainStaff), command.Id);

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Staff name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new ValidationException("Email address is required.");

        if (!string.IsNullOrWhiteSpace(req.TimeZoneId))
        {
            if (NodaTime.DateTimeZoneProviders.Tzdb.GetZoneOrNull(req.TimeZoneId.Trim()) == null)
            {
                throw new ValidationException($"INVALID_TIMEZONE: '{req.TimeZoneId}' is not a valid IANA timezone identifier.");
            }
            staff.TimeZoneId = req.TimeZoneId.Trim();
        }

        staff.Name = req.Name.Trim();
        staff.Email = req.Email.Trim();
        staff.Phone = req.Phone?.Trim();
        staff.Title = string.IsNullOrWhiteSpace(req.Title) ? "Staff Member" : req.Title.Trim();
        staff.Bio = req.Bio;
        staff.AvatarUrl = req.AvatarUrl;
        staff.IsActive = req.IsActive;
        staff.UpdatedAtUtc = DateTime.UtcNow;

        if (req.AssignedServiceIds != null)
        {
            _context.StaffServices.RemoveRange(staff.StaffServices);
            foreach (var serviceId in req.AssignedServiceIds.Distinct())
            {
                staff.StaffServices.Add(new StaffService
                {
                    StaffId = staff.Id,
                    ServiceId = serviceId,
                    TenantId = _tenantContext.TenantId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(staff);
    }

    public async Task<bool> Handle(ArchiveStaffCommand command, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (staff == null) throw new NotFoundException(nameof(DomainStaff), command.Id);

        staff.IsArchived = true;
        staff.IsActive = false;
        staff.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StaffDto> Handle(AssignStaffServicesCommand command, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == command.StaffId, cancellationToken);

        if (staff == null) throw new NotFoundException(nameof(DomainStaff), command.StaffId);

        _context.StaffServices.RemoveRange(staff.StaffServices);

        foreach (var serviceId in command.ServiceIds.Distinct())
        {
            staff.StaffServices.Add(new StaffService
            {
                StaffId = staff.Id,
                ServiceId = serviceId,
                TenantId = _tenantContext.TenantId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(staff);
    }

    public async Task<IReadOnlyList<WorkingHourDto>> Handle(SetWorkingHoursCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null) throw new NotFoundException(nameof(DomainStaff), request.StaffId);

        _context.WorkingHours.RemoveRange(staff.WorkingHours);

        var newHours = request.WorkingHours.Select(h => new WorkingHours
        {
            Id = Guid.NewGuid(),
            StaffId = staff.Id,
            TenantId = _tenantContext.TenantId,
            DayOfWeek = h.DayOfWeek,
            StartTime = h.StartTime,
            EndTime = h.EndTime
        }).ToList();

        _context.WorkingHours.AddRange(newHours);
        await _context.SaveChangesAsync(cancellationToken);

        return newHours.Select(h => new WorkingHourDto(h.Id, h.StaffId, h.DayOfWeek, h.StartTime, h.EndTime)).ToList();
    }

    public async Task<TimeOffDto> Handle(CreateTimeOffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);
        if (staff == null) throw new NotFoundException(nameof(DomainStaff), request.StaffId);

        var timeOff = new TimeOff
        {
            Id = Guid.NewGuid(),
            StaffId = staff.Id,
            TenantId = _tenantContext.TenantId,
            StartUtc = request.StartUtc.UtcDateTime,
            EndUtc = request.EndUtc.UtcDateTime,
            Reason = request.Reason
        };

        _context.TimeOffs.Add(timeOff);
        await _context.SaveChangesAsync(cancellationToken);

        return new TimeOffDto(timeOff.Id, timeOff.StaffId, timeOff.StartUtc, timeOff.EndUtc, timeOff.Reason);
    }

    private static StaffDto MapToDto(DomainStaff staff)
    {
        var workingHours = staff.WorkingHours?
            .Select(wh => new WorkingHourDto(wh.Id, wh.StaffId, wh.DayOfWeek, wh.StartTime, wh.EndTime))
            .ToList() ?? new List<WorkingHourDto>();

        var assignedServices = staff.StaffServices?.Select(ss => ss.ServiceId).ToList() ?? new List<Guid>();

        return new StaffDto(
            staff.Id,
            staff.TenantId,
            staff.Name,
            staff.Email,
            staff.Phone,
            staff.Title,
            staff.Bio,
            staff.AvatarUrl,
            staff.TimeZoneId,
            staff.IsActive,
            staff.IsArchived,
            assignedServices,
            workingHours,
            staff.CreatedAtUtc
        );
    }
}
