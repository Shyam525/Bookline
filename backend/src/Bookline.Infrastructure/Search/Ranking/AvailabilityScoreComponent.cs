using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

public class AvailabilityScoreComponent : IAvailabilityScoreComponent
{
    public double Evaluate(RankingContext context)
    {
        // Only include an availability ranking contribution when actual availability data is available
        if (!context.NextAvailableAt.HasValue)
        {
            return 0.0;
        }

        var delta = context.NextAvailableAt.Value - DateTime.UtcNow;

        if (delta.TotalHours <= 0) return 0.95; // Available now
        if (delta.TotalHours <= 24) return 1.0;  // Available today
        if (delta.TotalHours <= 48) return 0.80; // Available tomorrow
        if (delta.TotalDays <= 7) return 0.50;   // Available this week

        return 0.25; // Available later
    }
}
