using FluentValidation;
using MediatR;

namespace Bookline.Application.Staff.Commands;

public record BreakDto(TimeOnly StartTime, TimeOnly EndTime, string? Label = null);

public record WorkingHourDto(
    Guid Id,
    Guid StaffId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    List<BreakDto>? Breaks = null
);

public record BreakInput(TimeOnly StartTime, TimeOnly EndTime, string? Label = null);

public record WorkingHourInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    List<BreakInput>? Breaks = null
);

public record TimeOffDto(
    Guid Id,
    Guid StaffId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Reason
);

public record StaffDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Email,
    string? Phone,
    string? Title,
    string? Bio,
    string? AvatarUrl,
    string TimeZoneId,
    bool IsActive,
    bool IsArchived,
    List<Guid> AssignedServiceIds,
    IReadOnlyList<WorkingHourDto> WorkingHours,
    DateTime CreatedAtUtc
);

public record CreateStaffRequest(
    string Name,
    string Email,
    string? Phone = null,
    string? Title = "Staff Member",
    string? Bio = null,
    string? AvatarUrl = null,
    string TimeZoneId = "UTC",
    List<Guid>? AssignedServiceIds = null
);

public record UpdateStaffRequest(
    string Name,
    string Email,
    string? Phone = null,
    string? Title = "Staff Member",
    string? Bio = null,
    string? AvatarUrl = null,
    string TimeZoneId = "UTC",
    bool IsActive = true,
    List<Guid>? AssignedServiceIds = null
);

// Queries & Commands
public record GetAllStaffQuery(bool IncludeArchived = false) : IRequest<List<StaffDto>>;
public record GetPagedStaffQuery(
    bool IncludeArchived = false,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Sort = "name",
    string? Direction = "asc"
) : IRequest<Bookline.Application.Common.Models.PagedResult<StaffDto>>;
public record GetStaffByIdQuery(Guid Id) : IRequest<StaffDto>;
public record CreateStaffCommand(CreateStaffRequest Request) : IRequest<StaffDto>;
public record UpdateStaffCommand(Guid Id, UpdateStaffRequest Request) : IRequest<StaffDto>;
public record SetStaffStatusCommand(Guid Id, bool IsActive) : IRequest<StaffDto>;
public record SetStaffStatusRequest(bool IsActive);
public record ArchiveStaffCommand(Guid Id) : IRequest<bool>;
public record AssignStaffServicesCommand(Guid StaffId, List<Guid> ServiceIds) : IRequest<StaffDto>;

// Working Hours & Time Off Commands
public record SetWorkingHoursCommand(Guid StaffId, List<WorkingHourInput> WorkingHours) : IRequest<IReadOnlyList<WorkingHourDto>>;
public record CreateTimeOffCommand(Guid StaffId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason) : IRequest<TimeOffDto>;

// Validators
public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Staff name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");
    }
}

public class SetWorkingHoursCommandValidator : AbstractValidator<SetWorkingHoursCommand>
{
    public SetWorkingHoursCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();

        RuleForEach(x => x.WorkingHours).ChildRules(items =>
        {
            items.RuleFor(i => i.EndTime)
                .GreaterThan(i => i.StartTime)
                .WithMessage("End time must be after start time.");

            items.RuleFor(i => i.Breaks)
                .Must(HasNoOverlappingBreaks)
                .WithMessage("Breaks cannot overlap.")
                .When(i => i.Breaks != null && i.Breaks.Any());

            items.RuleFor(i => i)
                .Must(BreaksFallWithinWorkingHours)
                .WithMessage("Breaks must fall strictly within working hours.")
                .When(i => i.Breaks != null && i.Breaks.Any());
        });

        RuleFor(x => x.WorkingHours)
            .Must(HasNoOverlappingHours)
            .WithMessage("Working hours cannot contain overlapping intervals for the same day.");
    }

    private static bool HasNoOverlappingBreaks(List<BreakInput>? breaks)
    {
        if (breaks == null || breaks.Count <= 1) return true;

        var sorted = breaks.OrderBy(b => b.StartTime).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].EndTime <= sorted[i].StartTime)
            {
                return false;
            }
            if (i < sorted.Count - 1 && sorted[i].EndTime > sorted[i + 1].StartTime)
            {
                return false;
            }
        }
        return true;
    }

    private static bool BreaksFallWithinWorkingHours(WorkingHourInput input)
    {
        if (input.Breaks == null || !input.Breaks.Any()) return true;

        foreach (var b in input.Breaks)
        {
            if (b.StartTime < input.StartTime || b.EndTime > input.EndTime)
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasNoOverlappingHours(List<WorkingHourInput>? hours)
    {
        if (hours == null || hours.Count <= 1) return true;

        var grouped = hours.GroupBy(h => h.DayOfWeek);
        foreach (var group in grouped)
        {
            var sorted = group.OrderBy(h => h.StartTime).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].EndTime > sorted[i + 1].StartTime)
                {
                    return false;
                }
            }
        }
        return true;
    }
}

public class CreateTimeOffCommandValidator : AbstractValidator<CreateTimeOffCommand>
{
    public CreateTimeOffCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .WithMessage("End time must be after start time.");
    }
}
