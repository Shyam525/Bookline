using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

public class GeographicScoreComponent : IGeographicScoreComponent
{
    public double Evaluate(RankingContext context)
    {
        // Never fabricate distance if no reference coordinates were provided
        if (!context.DistanceMeters.HasValue)
        {
            // If customer explicitly filtered by area/neighborhood, credit match even without GPS
            if (!string.IsNullOrWhiteSpace(context.RequestedArea))
            {
                var area = context.RequestedArea.Trim();
                bool matchesArea = context.CandidateAddress.Contains(area, StringComparison.OrdinalIgnoreCase) ||
                                  context.CandidateName.Contains(area, StringComparison.OrdinalIgnoreCase);
                return matchesArea ? 0.90 : 0.50;
            }

            return 0.50; // Neutral baseline when location is city-wide or unbounded
        }

        double distanceKm = Math.Max(0.0, context.DistanceMeters.Value / 1000.0);

        // Rational decay curve: 1 / (1 + (d / D0)) where D0 = 8.0 km
        double proximityScore = 1.0 / (1.0 + (distanceKm / 8.0));

        // Area alignment bonus: if candidate is inside the user's explicitly requested area
        if (!string.IsNullOrWhiteSpace(context.RequestedArea))
        {
            var area = context.RequestedArea.Trim();
            bool matchesArea = context.CandidateAddress.Contains(area, StringComparison.OrdinalIgnoreCase) ||
                              context.CandidateName.Contains(area, StringComparison.OrdinalIgnoreCase);
            if (matchesArea)
            {
                proximityScore = Math.Min(1.0, proximityScore + 0.15);
            }
        }

        return Math.Clamp(proximityScore, 0.0, 1.0);
    }
}
