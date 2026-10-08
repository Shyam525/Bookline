namespace Bookline.Application.Common.Interfaces;

public record SearchIntentResult(
    string RawQuery,
    string? ExtractedService,
    string? ExtractedCategory,
    string? ExtractedLocation,
    string? ExtractedArea,
    string? ExtractedPostcode,
    string? ExtractedIntent, // "top_rated", "near_me", "lowest_price", "earliest"
    string CleanQuery
);

public interface ISearchIntentService
{
    SearchIntentResult ParseIntent(string query);
}
