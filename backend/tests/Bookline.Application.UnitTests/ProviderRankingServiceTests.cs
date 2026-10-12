using Bookline.Application.Common.Interfaces;
using Bookline.Infrastructure.Search.Ranking;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ProviderRankingServiceTests
{
    private readonly RelevanceScoreComponent _relevanceComponent = new();
    private readonly GeographicScoreComponent _geoComponent = new();
    private readonly QualityScoreComponent _qualityComponent = new();
    private readonly TrustScoreComponent _trustComponent = new();
    private readonly AvailabilityScoreComponent _availabilityComponent = new();
    private readonly ProviderRankingService _rankingService;

    public ProviderRankingServiceTests()
    {
        _rankingService = new ProviderRankingService(
            _relevanceComponent,
            _geoComponent,
            _qualityComponent,
            _trustComponent,
            _availabilityComponent
        );
    }

    [Fact]
    public void QualityComponent_BayesianDamping_EstablishedVenueOutranksSingleReviewFiveStar()
    {
        // 1. Established venue: 120 reviews, 4.9 average rating
        double scoreEstablished = _qualityComponent.CalculateBayesianSmoothedRating(4.9, 120);

        // 2. New venue: 1 review, 5.0 average rating
        double scoreSingleReview = _qualityComponent.CalculateBayesianSmoothedRating(5.0, 1);

        // Assert: 120 reviews at 4.9 must significantly outrank 1 review at 5.0
        // Formula: (4.9 * 120 + 4.0 * 5) / 125 = 4.864
        // vs (5.0 * 1 + 4.0 * 5) / 6 = 4.167
        Assert.True(scoreEstablished > scoreSingleReview,
            $"Established venue score ({scoreEstablished:F3}) should beat single review score ({scoreSingleReview:F3})");
        Assert.True(scoreEstablished > 4.85);
        Assert.True(scoreSingleReview < 4.25);
    }

    [Fact]
    public void QualityComponent_UnreviewedVenue_DoesNotFabricateStars()
    {
        var context = CreateTestContext(
            rating: null,
            reviewCount: 0
        );

        double score = _qualityComponent.Evaluate(context);

        // Returns unreviewed baseline score (not 0, not 5.0)
        Assert.True(score > 0.3 && score < 0.6);
    }

    [Fact]
    public void RelevanceComponent_ExactServiceMatch_OutranksMissingService()
    {
        var contextWithService = CreateTestContext(
            requestedService: "Reformer Pilates",
            services: new[] { "Reformer Pilates", "HIIT Training" }
        );

        var contextWithoutService = CreateTestContext(
            requestedService: "Reformer Pilates",
            services: new[] { "Haircut", "Beard Trim" }
        );

        double scoreWith = _relevanceComponent.Evaluate(contextWithService);
        double scoreWithout = _relevanceComponent.Evaluate(contextWithoutService);

        Assert.True(scoreWith > 0.9, $"Exact service match should score high, got {scoreWith}");
        Assert.True(scoreWithout < 0.2, $"Missing requested service should be penalized, got {scoreWithout}");
    }

    [Fact]
    public void RecommendedSort_RelevanceGating_RelevantProviderOutranksIrrelevantFiveStar()
    {
        // Provider A: Offers "Reformer Pilates", Rating 4.6 (50 reviews)
        var providerPilates = CreateTestContext(
            requestedService: "Reformer Pilates",
            services: new[] { "Reformer Pilates" },
            rating: 4.6,
            reviewCount: 50,
            distanceMeters: 2000.0
        );

        // Provider B: Luxury Salon (offers Haircut), Rating 5.0 (200 reviews), but does NOT offer Pilates
        var providerSalon = CreateTestContext(
            requestedService: "Reformer Pilates",
            services: new[] { "Haircut & Blowout", "Hair Coloring" },
            rating: 5.0,
            reviewCount: 200,
            distanceMeters: 500.0
        );

        var evaluationPilates = _rankingService.Evaluate(providerPilates);
        var evaluationSalon = _rankingService.Evaluate(providerSalon);

        // Core Requirement: A provider that does not offer the requested service must not outrank a relevant provider
        Assert.True(evaluationPilates.TotalWeightedScore > evaluationSalon.TotalWeightedScore,
            $"Relevant Pilates provider ({evaluationPilates.TotalWeightedScore:F3}) must outrank irrelevant Salon ({evaluationSalon.TotalWeightedScore:F3})");
        Assert.Contains("Relevance Gated", evaluationSalon.Explanation);
    }

    [Fact]
    public void GeographicComponent_DecaysWithDistance_AndPreservesNeutralWhenDistanceAbsent()
    {
        var nearContext = CreateTestContext(distanceMeters: 1000.0);   // 1 km
        var midContext = CreateTestContext(distanceMeters: 8000.0);    // 8 km
        var farContext = CreateTestContext(distanceMeters: 30000.0);   // 30 km
        var absentContext = CreateTestContext(distanceMeters: null);   // Distance absent

        double nearScore = _geoComponent.Evaluate(nearContext);
        double midScore = _geoComponent.Evaluate(midContext);
        double farScore = _geoComponent.Evaluate(farContext);
        double absentScore = _geoComponent.Evaluate(absentContext);

        Assert.True(nearScore > midScore, "Proximity score should strictly decrease with distance");
        Assert.True(midScore > farScore, "Proximity score should strictly decrease with distance");
        Assert.Equal(0.50, absentScore); // Neutral score, no distance fabrication
    }

    [Fact]
    public void RankingEvaluation_DeterministicForEquivalentInputs()
    {
        var context1 = CreateTestContext(
            queryText: "facial in bodakdev",
            requestedService: "Facial",
            requestedArea: "Bodakdev",
            services: new[] { "Hydrafacial", "Anti-Aging Facial" },
            rating: 4.8,
            reviewCount: 75,
            distanceMeters: 1500.0
        );

        var context2 = CreateTestContext(
            queryText: "facial in bodakdev",
            requestedService: "Facial",
            requestedArea: "Bodakdev",
            services: new[] { "Hydrafacial", "Anti-Aging Facial" },
            rating: 4.8,
            reviewCount: 75,
            distanceMeters: 1500.0
        );

        var result1 = _rankingService.Evaluate(context1);
        var result2 = _rankingService.Evaluate(context2);

        Assert.Equal(result1.TotalWeightedScore, result2.TotalWeightedScore);
        Assert.Equal(result1.RelevanceScore, result2.RelevanceScore);
        Assert.Equal(result1.GeographicScore, result2.GeographicScore);
        Assert.Equal(result1.QualityScore, result2.QualityScore);
        Assert.Equal(result1.TrustScore, result2.TrustScore);
    }

    [Fact]
    public void RankingEvaluation_AvailabilityOmittedWhenUnavailable_ReweightsCleanly()
    {
        var contextWithAvail = CreateTestContext(nextAvailable: DateTime.UtcNow.AddHours(4));
        var contextWithoutAvail = CreateTestContext(nextAvailable: null);

        var evalWith = _rankingService.Evaluate(contextWithAvail);
        var evalWithout = _rankingService.Evaluate(contextWithoutAvail);

        Assert.Contains("5-Signal Model", evalWith.Explanation);
        Assert.Contains("Availability Omitted", evalWithout.Explanation);
        Assert.Equal(0.0, evalWithout.AvailabilityScore);
    }

    private static RankingContext CreateTestContext(
        string? queryText = null,
        string? requestedService = null,
        string? requestedArea = null,
        string[]? services = null,
        double? rating = 4.8,
        int reviewCount = 60,
        double? distanceMeters = 2500.0,
        DateTime? nextAvailable = null)
    {
        return new RankingContext(
            QueryText: queryText,
            RequestedService: requestedService,
            RequestedCategory: null,
            RequestedArea: requestedArea,
            RequestedCity: "Ahmedabad",
            CandidateName: "Test Venue",
            CandidateCategory: "Beauty & Wellness",
            CandidateBusinessType: "Salon",
            CandidateDescription: "Full service beauty and wellness salon in Ahmedabad offering luxury treatments.",
            CandidateCity: "Ahmedabad",
            CandidateAddress: "Bodakdev, Ahmedabad",
            CandidateServices: services ?? new[] { "Haircut", "Facial" },
            DistanceMeters: distanceMeters,
            AverageRating: rating,
            ReviewCount: reviewCount,
            IsVerified: true,
            IsPublished: true,
            HasLogo: true,
            HasCoverImage: true,
            HasDetailedDescription: true,
            HasFullContactInfo: true,
            NextAvailableAt: nextAvailable
        );
    }
}
