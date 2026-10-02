using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Staff.Commands;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Staff.Handlers;

public record GetStaffByIdQuery(Guid Id) : IRequest<StaffDto>;

public record GetStaffQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<StaffDto>>;

public class StaffCommandHandler :
    IRequestHandler<CreateStaffCommand, StaffDto>,
    IRequestHandler<SetWorkingHoursCommand, IReadOnlyList<WorkingHourDto>>,
    IRequestHandler<CreateTimeOffCommand, TimeOffDto>
{
    private readonly IApplicationDbContext _dbContext;

    public StaffCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StaffDto> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TimeZoneId = request.TimeZoneId,
            IsActive = true
        };

        _dbContext.Staff.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StaffDto(staff.Id, staff.Name, staff.TimeZoneId, staff.IsActive, Array.Empty<WorkingHourDto>());
    }

    public async Task<IReadOnlyList<WorkingHourDto>> Handle(SetWorkingHoursCommand request, CancellationToken cancellationToken)
    {
        var staff = await _dbContext.Staff
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Staff), request.StaffId);
        }

        _dbContext.WorkingHours.RemoveRange(staff.WorkingHours);

        var newHours = request.WorkingHours.Select(h => new WorkingHours
        {
            Id = Guid.NewGuid(),
            StaffId = staff.Id,
            DayOfWeek = h.DayOfWeek,
            StartTime = h.StartTime,
            EndTime = h.EndTime
        }).ToList();

        _dbContext.WorkingHours.AddRange(newHours);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newHours.Select(h => new WorkingHourDto(h.Id, h.StaffId, h.DayOfWeek, h.StartTime, h.EndTime)).ToList();
    }

    public async Task<TimeOffDto> Handle(CreateTimeOffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _dbContext.Staff.FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Staff), request.StaffId);
        }

        var timeOff = new TimeOff
        {
            Id = Guid.NewGuid(),
            StaffId = staff.Id,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            Reason = request.Reason
        };

        _dbContext.TimeOffs.Add(timeOff);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TimeOffDto(timeOff.Id, timeOff.StaffId, timeOff.StartUtc, timeOff.EndUtc, timeOff.Reason);
    }
}

public class StaffQueryHandler :
    IRequestHandler<GetStaffByIdQuery, StaffDto>,
    IRequestHandler<GetStaffQuery, PagedResult<StaffDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public StaffQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StaffDto> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var staff = await _dbContext.Staff
            .AsNoTracking()
            .Include(s => s.WorkingHours)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Staff), request.Id);
        }

        var workingHourDtos = staff.WorkingHours
            .Select(wh => new WorkingHourDto(wh.Id, wh.StaffId, wh.DayOfWeek, wh.StartTime, wh.EndTime))
            .ToList();

        return new StaffDto(staff.Id, staff.Name, staff.TimeZoneId, staff.IsActive, workingHourDtos);
    }

    public async Task<PagedResult<StaffDto>> Handle(GetStaffQuery request, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery(request.Page, request.PageSize);

        var query = _dbContext.Staff.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .Select(s => new StaffDto(
                s.Id,
                s.Name,
                s.TimeZoneId,
                s.IsActive,
                s.WorkingHours.Select(wh => new WorkingHourDto(wh.Id, wh.StaffId, wh.DayOfWeek, wh.StartTime, wh.EndTime)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<StaffDto>(items, totalCount, pagedQuery.Page, pagedQuery.PageSize);
    }
}
