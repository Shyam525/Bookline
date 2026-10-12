namespace Bookline.Application.Common.Interfaces;

/// <summary>
/// Context evaluated by the ranking strategy for a specific candidate provider location.
/// Only utilizes signals the system actually possesses.
/// </summary>
public record RankingContext(
    string? QueryText,
    string? RequestedService,
    string? RequestedCategory,
    string? RequestedArea,
    string? RequestedCity,
    string CandidateName,
    string CandidateCategory,
    string CandidateBusinessType,
    string CandidateDescription,
    string CandidateCity,
    string CandidateAddress,
    IReadOnlyList<string> CandidateServices,
    double? DistanceMeters,
    double? AverageRating,
    int ReviewCount,
    bool IsVerified,
    bool IsPublished,
    bool HasLogo,
    bool HasCoverImage,
    bool HasDetailedDescription,
    bool HasFullContactInfo,
    DateTime? NextAvailableAt
);

public record RankingScoreBreakdown(
    double RelevanceScore,
    double GeographicScore,
    double QualityScore,
    double TrustScore,
    double AvailabilityScore,
    double TotalWeightedScore,
    string Explanation
);

/// <summary>
/// Evaluates text, service, category, and area relevance.
/// Acts as the primary relevance gate in Recommended sort.
/// </summary>
public interface IRelevanceScoreComponent
{
    double Evaluate(RankingContext context);
}

/// <summary>
/// Evaluates geographic proximity and neighborhood alignment without fabricating distances.
/// </summary>
public interface IGeographicScoreComponent
{
    double Evaluate(RankingContext context);
}

/// <summary>
/// Evaluates aggregated review data using Bayesian damping so established high-volume
/// venues are not outranked by single-review 5.0 venues.
/// </summary>
public interface IQualityScoreComponent
{
    double Evaluate(RankingContext context);
    double CalculateBayesianSmoothedRating(double? rating, int reviewCount);
}

/// <summary>
/// Evaluates provider verification status, completeness of public storefront, and platform trust.
/// </summary>
public interface ITrustScoreComponent
{
    double Evaluate(RankingContext context);
}

/// <summary>
/// Evaluates actual appointment availability without fabricating non-existent slots.
/// </summary>
public interface IAvailabilityScoreComponent
{
    double Evaluate(RankingContext context);
}

/// <summary>
/// Versioned, multi-signal ranking service for provider discovery.
/// Combines independent, testable scoring components into deterministic rankings.
/// </summary>
public interface IProviderRankingService
{
    string Version { get; }

    RankingScoreBreakdown Evaluate(RankingContext context);

    double CalculateBayesianSmoothedRating(double? rating, int reviewCount);
}
