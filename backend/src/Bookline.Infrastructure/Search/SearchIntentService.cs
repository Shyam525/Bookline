using System.Text.RegularExpressions;
using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Search;

public class SearchIntentService : ISearchIntentService
{
    private static readonly Dictionary<string, (string Category, string Service)> ServiceDictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        ["salon"] = ("Beauty & Wellness", "Salon"),
        ["haircut"] = ("Beauty & Wellness", "Haircut"),
        ["hair"] = ("Beauty & Wellness", "Hair Styling"),
        ["balayage"] = ("Beauty & Wellness", "Balayage"),
        ["keratin"] = ("Beauty & Wellness", "Keratin Ritual"),
        ["blowout"] = ("Beauty & Wellness", "Blowout"),
        ["facial"] = ("Beauty & Wellness", "Facial"),
        ["hydrafacial"] = ("Beauty & Wellness", "Hydrafacial"),
        ["skincare"] = ("Beauty & Wellness", "Skincare Treatment"),
        ["massage"] = ("Beauty & Wellness", "Massage"),
        ["aromatherapy"] = ("Beauty & Wellness", "Aromatherapy"),
        ["spa"] = ("Beauty & Wellness", "Spa Treatment"),
        ["dentist"] = ("Healthcare & Clinics", "Dental Consultation"),
        ["dental"] = ("Healthcare & Clinics", "Dental Care"),
        ["teeth whitening"] = ("Healthcare & Clinics", "Teeth Whitening"),
        ["pilates"] = ("Fitness & Training", "Pilates"),
        ["reformer"] = ("Fitness & Training", "Reformer Pilates"),
        ["yoga"] = ("Fitness & Training", "Yoga Session"),
        ["gym"] = ("Fitness & Training", "Fitness Training"),
        ["fitness"] = ("Fitness & Training", "Personal Training"),
        ["photographer"] = ("Photography & Media", "Photography"),
        ["photography"] = ("Photography & Media", "Studio Photography"),
        ["headshot"] = ("Photography & Media", "Executive Headshot"),
        ["lawyer"] = ("Professional Services", "Legal Consultation"),
        ["legal"] = ("Professional Services", "Legal Counsel"),
        ["notary"] = ("Professional Services", "Notary Services")
    };

    private static readonly Dictionary<string, string> KnownAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Satellite"] = "Ahmedabad",
        ["Bodakdev"] = "Ahmedabad",
        ["Navrangpura"] = "Ahmedabad",
        ["Vastrapur"] = "Ahmedabad",
        ["Sindhu Bhavan"] = "Ahmedabad",
        ["Bandra"] = "Mumbai",
        ["Andheri"] = "Mumbai",
        ["Juhu"] = "Mumbai",
        ["Koramangala"] = "Bangalore",
        ["Indiranagar"] = "Bangalore",
        ["Whitefield"] = "Bangalore"
    };

    private static readonly HashSet<string> KnownCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ahmedabad", "Mumbai", "Bangalore", "Bengaluru", "Surat", "Rajkot", "Vadodara", "Pune", "Delhi"
    };

    private static readonly Regex PostcodeRegex = new(@"\b\d{6}\b", RegexOptions.Compiled);

    public SearchIntentResult ParseIntent(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchIntentResult(string.Empty, null, null, null, null, null, null, string.Empty);
        }

        var raw = query.Trim();
        var working = raw;

        string? extractedIntent = null;
        string? extractedLocation = null;
        string? extractedArea = null;
        string? extractedPostcode = null;
        string? extractedService = null;
        string? extractedCategory = null;

        // 1. Postcode extraction (e.g. 380015)
        var postcodeMatch = PostcodeRegex.Match(working);
        if (postcodeMatch.Success)
        {
            extractedPostcode = postcodeMatch.Value;
            working = working.Replace(extractedPostcode, " ", StringComparison.OrdinalIgnoreCase);
        }

        // 2. Intent extraction
        if (Regex.IsMatch(working, @"\b(best|top|highest rated|top rated|finest)\b", RegexOptions.IgnoreCase))
        {
            extractedIntent = "top_rated";
            working = Regex.Replace(working, @"\b(best|top rated|highest rated|top|finest)\b", " ", RegexOptions.IgnoreCase);
        }
        else if (Regex.IsMatch(working, @"\b(near me|nearby|closest|around me)\b", RegexOptions.IgnoreCase))
        {
            extractedIntent = "near_me";
            working = Regex.Replace(working, @"\b(near me|nearby|closest|around me)\b", " ", RegexOptions.IgnoreCase);
        }
        else if (Regex.IsMatch(working, @"\b(cheap|affordable|low price|budget)\b", RegexOptions.IgnoreCase))
        {
            extractedIntent = "lowest_price";
            working = Regex.Replace(working, @"\b(cheap|affordable|low price|budget)\b", " ", RegexOptions.IgnoreCase);
        }
        else if (Regex.IsMatch(working, @"\b(today|now|available today|urgent)\b", RegexOptions.IgnoreCase))
        {
            extractedIntent = "earliest";
            working = Regex.Replace(working, @"\b(available today|today|now|urgent)\b", " ", RegexOptions.IgnoreCase);
        }

        // 3. Area Extraction (e.g. "Satellite", "Bodakdev", "Bandra")
        foreach (var (area, parentCity) in KnownAreas)
        {
            if (Regex.IsMatch(working, $@"\b{Regex.Escape(area)}\b", RegexOptions.IgnoreCase))
            {
                extractedArea = area;
                extractedLocation = parentCity;
                working = Regex.Replace(working, $@"\b{Regex.Escape(area)}\b", " ", RegexOptions.IgnoreCase);
                break;
            }
        }

        // 4. City Extraction (e.g. "Ahmedabad", "Rajkot", "Mumbai")
        if (extractedLocation == null)
        {
            foreach (var city in KnownCities)
            {
                if (Regex.IsMatch(working, $@"\b{Regex.Escape(city)}\b", RegexOptions.IgnoreCase))
                {
                    extractedLocation = city.Equals("Bengaluru", StringComparison.OrdinalIgnoreCase) ? "Bangalore" : city;
                    working = Regex.Replace(working, $@"\b{Regex.Escape(city)}\b", " ", RegexOptions.IgnoreCase);
                    break;
                }
            }
        }

        // Clean prepositions ("in", "at", "near", "around")
        working = Regex.Replace(working, @"\b(in|near|at|around|for|by)\b", " ", RegexOptions.IgnoreCase);

        // 5. Service & Category Extraction
        foreach (var (term, mapping) in ServiceDictionary)
        {
            if (Regex.IsMatch(working, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            {
                extractedService = mapping.Service;
                extractedCategory = mapping.Category;
                working = Regex.Replace(working, $@"\b{Regex.Escape(term)}\b", " ", RegexOptions.IgnoreCase);
                break;
            }
        }

        // Clean extra whitespace
        var cleanQuery = Regex.Replace(working, @"\s+", " ").Trim();

        return new SearchIntentResult(
            RawQuery: raw,
            ExtractedService: extractedService,
            ExtractedCategory: extractedCategory,
            ExtractedLocation: extractedLocation,
            ExtractedArea: extractedArea,
            ExtractedPostcode: extractedPostcode,
            ExtractedIntent: extractedIntent,
            CleanQuery: cleanQuery
        );
    }
}
