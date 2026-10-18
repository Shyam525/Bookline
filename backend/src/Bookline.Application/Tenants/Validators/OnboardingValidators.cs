using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
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

            When(x => x.Request.HoldDurationMinutes.HasValue, () =>
            {
                RuleFor(x => x.Request.HoldDurationMinutes!.Value)
                    .InclusiveBetween(1, 60)
                    .WithMessage("HoldDurationMinutes must be between 1 and 60 minutes.");
            });

            When(x => x.Request.MinimumNoticeHours.HasValue, () =>
            {
                RuleFor(x => x.Request.MinimumNoticeHours!.Value)
                    .InclusiveBetween(0, 168)
                    .WithMessage("MinimumNoticeHours must be between 0 and 168 hours.");
            });

            When(x => x.Request.BookingHorizonDays.HasValue, () =>
            {
                RuleFor(x => x.Request.BookingHorizonDays!.Value)
                    .InclusiveBetween(1, 365)
                    .WithMessage("BookingHorizonDays must be between 1 and 365 days.");
            });

            When(x => x.Request.CancellationWindowHours.HasValue, () =>
            {
                RuleFor(x => x.Request.CancellationWindowHours!.Value)
                    .InclusiveBetween(0, 168)
                    .WithMessage("CancellationWindowHours must be between 0 and 168 hours.");
            });

            When(x => x.Request.StartTime.HasValue && x.Request.EndTime.HasValue, () =>
            {
                RuleFor(x => x.Request.EndTime!.Value)
                    .GreaterThan(x => x.Request.StartTime!.Value)
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
