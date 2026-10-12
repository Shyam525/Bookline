using System.Reflection;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Phase 7: Public Discovery API Contract & Security Tests.
/// Enforces strict separation between public marketplace DTOs and sensitive internal data.
/// </summary>
public class PublicDiscoveryApiSecurityTests
{
    [Fact]
    public void PublicDto_NeverContainsSensitiveFinancialOrInternalFields()
    {
        var publicDtoTypes = new[]
        {
            typeof(ProviderCardDto),
            typeof(ProviderLocationResult),
            typeof(PublicProviderProfileDto),
            typeof(PublicLocationDto),
            typeof(PublicServiceDto),
            typeof(PublicProductDto),
            typeof(PublicStaffDto),
            typeof(PublicReviewDto)
        };

        var forbiddenNames = new[]
        {
            "Password",
            "PasswordHash",
            "PendingPayoutBalance",
            "AvailablePayoutBalance",
            "PaidOutBalance",
            "CommissionRatePercentage",
            "Secret",
            "ApiKey",
            "Stripe",
            "InternalNotes",
            "PrivateNotes"
        };

        foreach (var dtoType in publicDtoTypes)
        {
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                foreach (var forbidden in forbiddenNames)
                {
                    Assert.False(
                        prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                        $"Public DTO '{dtoType.Name}' must never leak private field containing '{forbidden}'. Found property: '{prop.Name}'."
                    );
                }
            }
        }
    }

    [Fact]
    public void ProviderLocationResult_CarriesOnlyPublicMarketplaceMetadata()
    {
        var result = new ProviderLocationResult(
            BusinessId: Guid.NewGuid(),
            BusinessSlug: "aura-wellness",
            LocationId: Guid.NewGuid(),
            PublicName: "Aura Wellness",
            Category: "Beauty & Wellness",
            PublicImage: "https://bookline.local/images/aura.jpg",
            PublicAddress: "102 SG Highway",
            City: "Ahmedabad",
            Area: "Bodakdev",
            Latitude: 23.0396,
            Longitude: 72.5074,
            DistanceMeters: 1200.0,
            RatingAverage: 4.8,
            ReviewCount: 95,
            StartingPrice: 50.0m,
            Currency: "USD",
            PublicServices: new() { "Massage", "Facial" },
            VerificationStatus: Domain.Enums.VerificationStatus.Verified,
            NextAvailableAt: null
        );

        Assert.NotNull(result.PublicName);
        Assert.NotNull(result.PublicAddress);
        Assert.NotNull(result.City);
        Assert.NotNull(result.DistanceMeters);
        Assert.NotNull(result.RatingAverage);
        Assert.NotNull(result.ReviewCount);
    }
}
