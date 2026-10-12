using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

/// <summary>
/// Phase 6: Multi-Signal Provider Ranking Service.
/// Version 1.0.0
/// Combines 5 independent, testable components: Relevance, Geographic, Quality, Trust, Availability.
/// </summary>
public class ProviderRankingService : IProviderRankingService
{
    private readonly IRelevanceScoreComponent _relevanceComponent;
    private readonly IGeographicScoreComponent _geographicComponent;
    private readonly IQualityScoreComponent _qualityComponent;
    private readonly ITrustScoreComponent _trustComponent;
    private readonly IAvailabilityScoreComponent _availabilityComponent;

    public string Version => "1.0.0";

    public ProviderRankingService(
        IRelevanceScoreComponent relevanceComponent,
        IGeographicScoreComponent geographicComponent,
        IQualityScoreComponent qualityComponent,
        ITrustScoreComponent trustComponent,
        IAvailabilityScoreComponent availabilityComponent)
    {
        _relevanceComponent = relevanceComponent;
        _geographicComponent = geographicComponent;
        _qualityComponent = qualityComponent;
        _trustComponent = trustComponent;
        _availabilityComponent = availabilityComponent;
    }

    public RankingScoreBreakdown Evaluate(RankingContext context)
    {
        double relevance = _relevanceComponent.Evaluate(context);
        double geographic = _geographicComponent.Evaluate(context);
        double quality = _qualityComponent.Evaluate(context);
        double trust = _trustComponent.Evaluate(context);
        double availability = _availabilityComponent.Evaluate(context);

        double totalScore;
        string explanation;

        if (context.NextAvailableAt.HasValue)
        {
            // Full 5-signal weighting
            double wRelevance = 0.35;
            double wGeo = 0.20;
            double wQuality = 0.20;
            double wTrust = 0.15;
            double wAvail = 0.10;

            totalScore = (relevance * wRelevance) +
                         (geographic * wGeo) +
                         (quality * wQuality) +
                         (trust * wTrust) +
                         (availability * wAvail);

            explanation = $"5-Signal Model (Rel: {relevance:F2}, Geo: {geographic:F2}, Qual: {quality:F2}, Trust: {trust:F2}, Avail: {availability:F2})";
        }
        else
        {
            // Availability signal omitted and documented; weights renormalized
            double wRelevance = 0.38;
            double wGeo = 0.22;
            double wQuality = 0.23;
            double wTrust = 0.17;

            totalScore = (relevance * wRelevance) +
                         (geographic * wGeo) +
                         (quality * wQuality) +
                         (trust * wTrust);

            explanation = $"4-Signal Model [Availability Omitted] (Rel: {relevance:F2}, Geo: {geographic:F2}, Qual: {quality:F2}, Trust: {trust:F2})";
        }

        // Relevance Gating Rule:
        // A provider that completely misses the requested query/service (relevance < 0.20)
        // must never outrank a relevant provider simply because of a high rating score.
        if (relevance < 0.20)
        {
            totalScore *= 0.40; // Heavy gate penalty
            explanation += " [Relevance Gated]";
        }

        return new RankingScoreBreakdown(
            RelevanceScore: Math.Round(relevance, 4),
            GeographicScore: Math.Round(geographic, 4),
            QualityScore: Math.Round(quality, 4),
            TrustScore: Math.Round(trust, 4),
            AvailabilityScore: Math.Round(availability, 4),
            TotalWeightedScore: Math.Round(totalScore, 4),
            Explanation: explanation
        );
    }

    public double CalculateBayesianSmoothedRating(double? rating, int reviewCount)
    {
        return _qualityComponent.CalculateBayesianSmoothedRating(rating, reviewCount);
    }
}
