using Bookline.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Bookline.Application.Staff.Commands;

public record StaffDto(
    Guid Id,
    string Name,
    string TimeZoneId,
    bool IsActive,
    IReadOnlyList<WorkingHourDto> WorkingHours
);

public record WorkingHourDto(
    Guid Id,
    Guid StaffId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record TimeOffDto(
    Guid Id,
    Guid StaffId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Reason
);

public record CreateStaffCommand(string Name, string TimeZoneId) : IRequest<StaffDto>;

public record WorkingHourInput(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record SetWorkingHoursCommand(Guid StaffId, List<WorkingHourInput> WorkingHours) : IRequest<IReadOnlyList<WorkingHourDto>>;

public record CreateTimeOffCommand(Guid StaffId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason) : IRequest<TimeOffDto>;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Staff name is required.")
            .MaximumLength(100);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("TimeZoneId is required.");
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
        });

        RuleFor(x => x.WorkingHours)
            .Custom((hours, context) =>
            {
                var groupedByDay = hours.GroupBy(h => h.DayOfWeek);
                foreach (var group in groupedByDay)
                {
                    var sorted = group.OrderBy(h => h.StartTime).ToList();
                    for (int i = 0; i < sorted.Count - 1; i++)
                    {
                        if (sorted[i].EndTime > sorted[i + 1].StartTime)
                        {
                            context.AddFailure($"Overlapping working hours detected on {group.Key}.");
                            break;
                        }
                    }
                }
            });
    }
}

public class CreateTimeOffCommandValidator : AbstractValidator<CreateTimeOffCommand>
{
    public CreateTimeOffCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();

        RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .WithMessage("Time off end time must be after start time.");
    }
}
