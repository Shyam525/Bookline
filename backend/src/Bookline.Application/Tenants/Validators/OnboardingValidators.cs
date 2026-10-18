using Bookline.Application.Tenants.Commands;
using FluentValidation;
using NodaTime;

namespace Bookline.Application.Tenants.Validators;

public class SaveOnboardingStepCommandValidator : AbstractValidator<SaveOnboardingStepCommand>
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "INR", "AUD", "CAD", "JPY", "SGD", "NZD", "AED", "BHD", "KWD"
    };

    public SaveOnboardingStepCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Onboarding request cannot be null.");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Step)
                .InclusiveBetween(1, 8)
                .WithMessage("Onboarding step must be between 1 and 8.");

            When(x => !string.IsNullOrWhiteSpace(x.Request.TimeZoneId), () =>
            {
                RuleFor(x => x.Request.TimeZoneId)
                    .Must(BeValidIanaTimeZone)
                    .WithMessage("TimeZoneId must be a valid IANA timezone identifier (e.g., 'America/New_York', 'Asia/Kolkata', 'UTC').");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Request.Currency), () =>
            {
                RuleFor(x => x.Request.Currency)
                    .Must(c => SupportedCurrencies.Contains(c!.Trim()))
                    .WithMessage("Currency must be a supported 3-letter currency code (e.g., USD, EUR, GBP, INR, AED).");
            });

            RuleFor(x => x.Request.HoldDurationMinutes)
                .InclusiveBetween(1, 60)
                .When(x => x.Request.HoldDurationMinutes.HasValue)
                .WithMessage("HoldDurationMinutes must be between 1 and 60 minutes.");

            RuleFor(x => x.Request.MinimumNoticeHours)
                .InclusiveBetween(0, 168)
                .When(x => x.Request.MinimumNoticeHours.HasValue)
                .WithMessage("MinimumNoticeHours must be between 0 and 168 hours.");

            RuleFor(x => x.Request.BookingHorizonDays)
                .InclusiveBetween(1, 365)
                .When(x => x.Request.BookingHorizonDays.HasValue)
                .WithMessage("BookingHorizonDays must be between 1 and 365 days.");

            RuleFor(x => x.Request.CancellationWindowHours)
                .InclusiveBetween(0, 168)
                .When(x => x.Request.CancellationWindowHours.HasValue)
                .WithMessage("CancellationWindowHours must be between 0 and 168 hours.");

            When(x => x.Request.StartTime.HasValue && x.Request.EndTime.HasValue, () =>
            {
                RuleFor(x => x.Request.EndTime)
                    .GreaterThan(x => x.Request.StartTime)
                    .WithMessage("Operating hours EndTime must be after StartTime.");
            });
        });
    }

    private static bool BeValidIanaTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return false;
        return DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId.Trim()) != null;
    }
}
