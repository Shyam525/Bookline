using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

public class TrustScoreComponent : ITrustScoreComponent
{
    public double Evaluate(RankingContext context)
    {
        double score = 0.0;

        // 1. Verified Provider Status (40%)
        if (context.IsVerified)
        {
            score += 0.40;
        }

        // 2. Profile Completeness (40%)
        double completeness = 0.0;
        if (context.HasLogo) completeness += 0.10;
        if (context.HasCoverImage) completeness += 0.10;
        if (context.HasDetailedDescription) completeness += 0.10;
        if (context.HasFullContactInfo) completeness += 0.10;
        score += completeness;

        // 3. Operational Reliability / Published Status (20%)
        if (context.IsPublished)
        {
            score += 0.20;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }
}
