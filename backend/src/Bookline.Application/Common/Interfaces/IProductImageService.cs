namespace Bookline.Application.Common.Interfaces;

public record ImageValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    string? NormalizedUrl = null
);

public interface IProductImageService
{
    Task<ImageValidationResult> ValidateImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageValidationResult>> ValidateImageUrlsAsync(IEnumerable<string> imageUrls, CancellationToken cancellationToken = default);
    Task<string> NormalizeImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
}
