using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Discovery.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/public/businesses")]
[Route("api/v1/public/businesses")]
[EnableRateLimiting("discovery-limit")]
public class PublicBusinessesController : ControllerBase
{
    private readonly IPublicStorefrontService _storefrontService;

    public PublicBusinessesController(IPublicStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    /// <summary>
    /// Retrieves a complete, verified public storefront profile by business slug (Phase 15).
    /// Returns 404 for missing, unpublished, or suspended businesses without revealing internal notes.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicStorefrontResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusinessBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var storefront = await _storefrontService.GetStorefrontBySlugAsync(slug, cancellationToken);
        if (storefront == null)
        {
            return NotFound(new ApiErrorResponse(
                "BUSINESS_NOT_FOUND",
                "The requested business is currently unavailable or does not exist.",
                HttpContext.TraceIdentifier,
                404
            ));
        }

        return Ok(storefront);
    }

    /// <summary>
    /// Retrieves public catalog services for a business.
    /// </summary>
    [HttpGet("{slug}/services")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices(string slug, CancellationToken cancellationToken = default)
    {
        var services = await _storefrontService.GetServicesBySlugAsync(slug, cancellationToken);
        if (services == null)
        {
            return NotFound(new ApiErrorResponse("BUSINESS_NOT_FOUND", "Business not found.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(services);
    }

    /// <summary>
    /// Retrieves a single public service by business slug and service slug.
    /// </summary>
    [HttpGet("{slug}/services/{serviceSlug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServiceBySlug(string slug, string serviceSlug, CancellationToken cancellationToken = default)
    {
        var service = await _storefrontService.GetServiceBySlugAsync(slug, serviceSlug, cancellationToken);
        if (service == null)
        {
            return NotFound(new ApiErrorResponse("SERVICE_NOT_FOUND", "The requested service was not found or is unpublished.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(service);
    }

    /// <summary>
    /// Retrieves public products for a business.
    /// </summary>
    [HttpGet("{slug}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(string slug, CancellationToken cancellationToken = default)
    {
        var products = await _storefrontService.GetProductsBySlugAsync(slug, cancellationToken);
        if (products == null)
        {
            return NotFound(new ApiErrorResponse("BUSINESS_NOT_FOUND", "Business not found.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(products);
    }

    /// <summary>
    /// Retrieves public multi-location branches for a business.
    /// </summary>
    [HttpGet("{slug}/locations")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLocations(string slug, CancellationToken cancellationToken = default)
    {
        var locations = await _storefrontService.GetLocationsBySlugAsync(slug, cancellationToken);
        if (locations == null)
        {
            return NotFound(new ApiErrorResponse("BUSINESS_NOT_FOUND", "Business not found.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(locations);
    }

    /// <summary>
    /// Retrieves approved public reviews with pagination.
    /// </summary>
    [HttpGet("{slug}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var reviewsResult = await _storefrontService.GetReviewsBySlugAsync(slug, page, pageSize, cancellationToken);
        if (reviewsResult == null)
        {
            return NotFound(new ApiErrorResponse("BUSINESS_NOT_FOUND", "Business not found.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(new
        {
            Items = reviewsResult.Value.Items,
            TotalCount = reviewsResult.Value.TotalCount,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 50)
        });
    }

    /// <summary>
    /// Retrieves public operating/working hours for a business.
    /// </summary>
    [HttpGet("{slug}/hours")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHours(string slug, CancellationToken cancellationToken = default)
    {
        var hours = await _storefrontService.GetHoursBySlugAsync(slug, cancellationToken);
        if (hours == null)
        {
            return NotFound(new ApiErrorResponse("BUSINESS_NOT_FOUND", "Business not found.", HttpContext.TraceIdentifier, 404));
        }

        return Ok(hours);
    }
}
