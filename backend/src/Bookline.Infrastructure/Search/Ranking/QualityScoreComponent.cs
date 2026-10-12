using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

public class QualityScoreComponent : IQualityScoreComponent
{
    private const double PriorMean = 4.0;
    private const double PriorWeight = 5.0;

    public double Evaluate(RankingContext context)
    {
        if (context.ReviewCount <= 0 || !context.AverageRating.HasValue)
        {
            // Unreviewed provider baseline: fair entry score, does not fabricate fake stars
            return 0.45;
        }

        double smoothedRating = CalculateBayesianSmoothedRating(context.AverageRating.Value, context.ReviewCount);
        double normalizedRating = Math.Clamp(smoothedRating / 5.0, 0.0, 1.0);

        // Review volume confidence logarithmic scaling: log10(v + 1) / 2.0 (saturates around 100 reviews)
        double volumeConfidence = Math.Clamp(Math.Log10(context.ReviewCount + 1) / 2.0, 0.0, 1.0);

        return (normalizedRating * 0.70) + (volumeConfidence * 0.30);
    }

    /// <summary>
    /// Computes Bayesian smoothed rating: (R * v + C * m) / (v + m).
    /// Ensures 1-review 5.0 venues (score ~4.17) do not outrank established 120-review 4.9 venues (score ~4.86).
    /// </summary>
    public double CalculateBayesianSmoothedRating(double? rating, int reviewCount)
    {
        if (!rating.HasValue || reviewCount <= 0)
        {
            return PriorMean;
        }

        double v = reviewCount;
        double r = Math.Clamp(rating.Value, 1.0, 5.0);
        return ((r * v) + (PriorMean * PriorWeight)) / (v + PriorWeight);
    }
}
