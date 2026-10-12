using Bookline.Application.Discovery.Dtos;
using FluentValidation;

namespace Bookline.Application.Discovery.Validators;

public class DiscoveryQueryValidator : AbstractValidator<DiscoveryQuery>
{
    public const double MaxConfiguredRadiusMeters = 100_000.0; // 100 km
    public const int MaxAllowedPageSize = 50;

    public DiscoveryQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90.0 and +90.0 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180.0 and +180.0 degrees.");

        RuleFor(x => x.RadiusMeters)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxConfiguredRadiusMeters)
            .When(x => x.RadiusMeters.HasValue)
            .WithMessage($"Radius must be positive and not exceed {MaxConfiguredRadiusMeters:N0} meters.");

        RuleFor(x => x.ViewportBounds)
            .Must(bounds => bounds == null || bounds.IsValid())
            .WithMessage("Viewport bounds are invalid. SouthWestLatitude must be <= NorthEastLatitude, and coordinates within legal geographic ranges.");

        RuleFor(x => x.MinimumRating)
            .InclusiveBetween(0.0, 5.0)
            .When(x => x.MinimumRating.HasValue)
            .WithMessage("Minimum rating must be between 0.0 and 5.0 stars.");

        RuleFor(x => x.MaximumPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaximumPrice.HasValue)
            .WithMessage("Maximum price cannot be negative.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxAllowedPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxAllowedPageSize}. Unlimited provider queries are rejected.");
    }
}
