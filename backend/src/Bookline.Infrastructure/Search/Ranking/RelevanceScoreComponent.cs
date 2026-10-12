using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search.Ranking;

public class RelevanceScoreComponent : IRelevanceScoreComponent
{
    public double Evaluate(RankingContext context)
    {
        // If no text, service, or category filter specified, candidate is considered relevant baseline
        if (string.IsNullOrWhiteSpace(context.QueryText) &&
            string.IsNullOrWhiteSpace(context.RequestedService) &&
            string.IsNullOrWhiteSpace(context.RequestedCategory) &&
            string.IsNullOrWhiteSpace(context.RequestedArea))
        {
            return 0.85;
        }

        double score = 0.0;
        int activeSignals = 0;

        // 1. Explicit Service Match Gate (Highest priority in service-seeking marketplaces)
        if (!string.IsNullOrWhiteSpace(context.RequestedService))
        {
            activeSignals++;
            var srvReq = context.RequestedService.Trim();
            bool hasExactService = context.CandidateServices.Any(s =>
                s.Equals(srvReq, StringComparison.OrdinalIgnoreCase) ||
                s.Contains(srvReq, StringComparison.OrdinalIgnoreCase));

            if (hasExactService)
            {
                score += 1.0;
            }
            else
            {
                // Strict penalty: a provider that doesn't offer the requested service cannot score high on relevance
                score += 0.05;
            }
        }

        // 2. Query Text Relevance
        if (!string.IsNullOrWhiteSpace(context.QueryText))
        {
            activeSignals++;
            var q = context.QueryText.Trim();
            double textScore = 0.0;

            bool nameMatch = context.CandidateName.Contains(q, StringComparison.OrdinalIgnoreCase);
            bool serviceMatch = context.CandidateServices.Any(s => s.Contains(q, StringComparison.OrdinalIgnoreCase));
            bool categoryMatch = context.CandidateCategory.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                 context.CandidateBusinessType.Contains(q, StringComparison.OrdinalIgnoreCase);
            bool descMatch = context.CandidateDescription.Contains(q, StringComparison.OrdinalIgnoreCase);
            bool addressMatch = context.CandidateAddress.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                context.CandidateCity.Contains(q, StringComparison.OrdinalIgnoreCase);

            if (nameMatch) textScore = Math.Max(textScore, 1.0);
            if (serviceMatch) textScore = Math.Max(textScore, 0.95);
            if (categoryMatch) textScore = Math.Max(textScore, 0.85);
            if (descMatch) textScore = Math.Max(textScore, 0.70);
            if (addressMatch) textScore = Math.Max(textScore, 0.60);

            if (textScore == 0.0)
            {
                // Check token-level partial matches
                var tokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length > 0)
                {
                    int tokenMatches = tokens.Count(t =>
                        context.CandidateName.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        context.CandidateServices.Any(s => s.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                        context.CandidateCategory.Contains(t, StringComparison.OrdinalIgnoreCase));

                    textScore = Math.Clamp((double)tokenMatches / tokens.Length, 0.1, 0.75);
                }
            }

            score += Math.Max(0.1, textScore);
        }

        // 3. Category Match
        if (!string.IsNullOrWhiteSpace(context.RequestedCategory))
        {
            activeSignals++;
            var cat = context.RequestedCategory.Trim();
            bool catMatch = context.CandidateCategory.Contains(cat, StringComparison.OrdinalIgnoreCase) ||
                            context.CandidateBusinessType.Contains(cat, StringComparison.OrdinalIgnoreCase);
            score += catMatch ? 1.0 : 0.1;
        }

        // 4. Neighborhood/Area Match
        if (!string.IsNullOrWhiteSpace(context.RequestedArea))
        {
            activeSignals++;
            var area = context.RequestedArea.Trim();
            bool areaMatch = context.CandidateAddress.Contains(area, StringComparison.OrdinalIgnoreCase) ||
                             context.CandidateName.Contains(area, StringComparison.OrdinalIgnoreCase);
            score += areaMatch ? 1.0 : 0.3;
        }

        return activeSignals > 0 ? Math.Clamp(score / activeSignals, 0.0, 1.0) : 0.85;
    }
}
