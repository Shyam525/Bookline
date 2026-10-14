using System.Text.RegularExpressions;
using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Services;

public class ProductImageService : IProductImageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".svg", ".avif", ".gif"
    };

    private static readonly HashSet<string> TrustedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "images.unsplash.com",
        "unsplash.com",
        "res.cloudinary.com",
        "i.imgur.com",
        "images.pexels.com",
        "cdn.pixabay.com",
        "bookline.app",
        "localhost"
    };

    public Task<ImageValidationResult> ValidateImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return Task.FromResult(new ImageValidationResult(false, "Image URL cannot be empty."));
        }

        var trimmed = imageUrl.Trim();

        // Reject dangerous protocol injection
        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ImageValidationResult(false, "Unsupported or insecure image URL protocol."));
        }

        // Relative path support (e.g. /images/products/...)
        if (trimmed.StartsWith("/") && !trimmed.StartsWith("//"))
        {
            var extension = Path.GetExtension(trimmed.Split('?')[0]);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return Task.FromResult(new ImageValidationResult(false, $"Image must have a valid extension ({string.Join(", ", AllowedExtensions)})."));
            }

            return Task.FromResult(new ImageValidationResult(true, null, trimmed));
        }

        // Absolute URL validation
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Task.FromResult(new ImageValidationResult(false, "Image URL must be a valid HTTP or HTTPS address."));
        }

        // Check host or extension
        var pathExtension = Path.GetExtension(uri.AbsolutePath);
        var isTrustedDomain = TrustedDomains.Any(domain => uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        var hasValidExtension = !string.IsNullOrEmpty(pathExtension) && AllowedExtensions.Contains(pathExtension);

        if (!hasValidExtension && !isTrustedDomain)
        {
            return Task.FromResult(new ImageValidationResult(
                false,
                $"Image must end with a supported format ({string.Join(", ", AllowedExtensions)}) or come from a recognized CDN provider."
            ));
        }

        return Task.FromResult(new ImageValidationResult(true, null, uri.ToString()));
    }

    public async Task<IReadOnlyList<ImageValidationResult>> ValidateImageUrlsAsync(IEnumerable<string> imageUrls, CancellationToken cancellationToken = default)
    {
        var results = new List<ImageValidationResult>();
        foreach (var url in imageUrls)
        {
            results.Add(await ValidateImageUrlAsync(url, cancellationToken));
        }
        return results;
    }

    public Task<string> NormalizeImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.FromResult(string.Empty);

        return Task.FromResult(imageUrl.Trim());
    }
}
