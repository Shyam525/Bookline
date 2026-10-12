using Bookline.Application.Discovery.Dtos;

namespace Bookline.Application.Common.Interfaces;

public interface IPublicStorefrontService
{
    Task<PublicStorefrontResponse?> GetStorefrontBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicServiceDto>?> GetServicesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicProductDto>?> GetProductsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicLocationDto>?> GetLocationsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PublicReviewDto> Items, int TotalCount)?> GetReviewsBySlugAsync(string slug, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicBusinessHoursDto>?> GetHoursBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
