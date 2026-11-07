using System.Text.RegularExpressions;
using Bookline.Application.Tenants.DTOs;
using FluentValidation;
using MediatR;
using NodaTime;

namespace Bookline.Application.Tenants.Commands;

public record GetBusinessProfileQuery : IRequest<BusinessProfileDto>;

public record UpdateBusinessProfileCommand(UpdateBusinessProfileRequest Request) : IRequest<BusinessProfileDto>;

public record GetBusinessPoliciesQuery : IRequest<BusinessPoliciesDto>;

public record UpdateBusinessPoliciesCommand(UpdateBusinessPoliciesRequest Request) : IRequest<BusinessPoliciesDto>;

public record CheckSlugAvailabilityQuery(string Slug) : IRequest<CheckSlugAvailabilityResponse>;

public class UpdateBusinessProfileCommandValidator : AbstractValidator<UpdateBusinessProfileCommand>
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    public UpdateBusinessProfileCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Business name is required.")
            .MinimumLength(2).WithMessage("Business name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Business name cannot exceed 100 characters.");

        RuleFor(x => x.Request.Slug)
            .NotEmpty().WithMessage("Public URL slug is required.")
            .MinimumLength(3).WithMessage("Public URL slug must be at least 3 characters.")
            .MaximumLength(60).WithMessage("Public URL slug cannot exceed 60 characters.")
            .Matches(SlugRegex).WithMessage("Slug must contain only lowercase letters, numbers, and hyphens without consecutive or trailing hyphens.");

        RuleFor(x => x.Request.TimeZoneId)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(IsValidIanaTimeZone).WithMessage("Invalid IANA timezone identifier.");

        RuleFor(x => x.Request.Category)
            .NotEmpty().WithMessage("Business category is required.");

        RuleFor(x => x.Request.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Request.Area)
            .NotEmpty().WithMessage("Area/locality is required.");
    }

    private static bool IsValidIanaTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return false;
        return DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId.Trim()) != null;
    }
}

public class UpdateBusinessPoliciesCommandValidator : AbstractValidator<UpdateBusinessPoliciesCommand>
{
    public UpdateBusinessPoliciesCommandValidator()
    {
        RuleFor(x => x.Request.HoldDurationMinutes)
            .GreaterThan(0).WithMessage("Slot hold duration must be greater than zero minutes.");

        RuleFor(x => x.Request.MinimumNoticeHours)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum notice hours cannot be negative.");

        RuleFor(x => x.Request.BookingHorizonDays)
            .GreaterThan(0).WithMessage("Booking horizon must be at least 1 day.")
            .LessThanOrEqualTo(365).WithMessage("Booking horizon cannot exceed 365 days.");

        RuleFor(x => x.Request.CancellationWindowHours)
            .GreaterThanOrEqualTo(0).WithMessage("Cancellation notice window cannot be negative.");

        RuleFor(x => x.Request.ReschedulingWindowHours)
            .GreaterThanOrEqualTo(0).WithMessage("Rescheduling notice window cannot be negative.");

        When(x => x.Request.WeeklyHours != null, () =>
        {
            RuleForEach(x => x.Request.WeeklyHours!).ChildRules(item =>
            {
                item.When(d => d.IsOpen, () =>
                {
                    item.RuleFor(d => d.CloseTime)
                        .GreaterThan(d => d.OpenTime)
                        .WithMessage("Closing time must be after opening time.");
                });

                item.When(d => d.Breaks != null && d.Breaks.Any(), () =>
                {
                    item.RuleFor(d => d.Breaks)
                        .Must(HasNoOverlappingBusinessBreaks)
                        .WithMessage("Break periods cannot overlap.");

                    item.RuleFor(d => d)
                        .Must(BreaksWithinOperatingHours)
                        .WithMessage("Break periods must fall within daily operating opening and closing hours.");
                });
            });
        });
    }

    private static bool HasNoOverlappingBusinessBreaks(List<BusinessBreakPeriod>? breaks)
    {
        if (breaks == null || breaks.Count <= 1) return true;

        var sorted = breaks.OrderBy(b => b.StartTime).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].EndTime <= sorted[i].StartTime) return false;
            if (i < sorted.Count - 1 && sorted[i].EndTime > sorted[i + 1].StartTime) return false;
        }
        return true;
    }

    private static bool BreaksWithinOperatingHours(BusinessDayHours day)
    {
        if (day.Breaks == null || !day.Breaks.Any() || !day.IsOpen || !day.OpenTime.HasValue || !day.CloseTime.HasValue)
            return true;

        foreach (var b in day.Breaks)
        {
            if (b.StartTime < day.OpenTime.Value || b.EndTime > day.CloseTime.Value)
                return false;
        }
        return true;
    }
}
