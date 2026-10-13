using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Discovery.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/geo")]
[Route("api/v1/geo")]
[EnableRateLimiting("discovery-limit")]
public class GeoController : ControllerBase
{
    private readonly ILocationDiscoveryService _locationDiscoveryService;
    private readonly IGeocodingProvider _geocodingProvider;

    public GeoController(
        ILocationDiscoveryService locationDiscoveryService,
        IGeocodingProvider geocodingProvider)
    {
        _locationDiscoveryService = locationDiscoveryService;
        _geocodingProvider = geocodingProvider;
    }

    /// <summary>
    /// Searches for places, cities, neighborhoods, and postal codes.
    /// Preserves geographic viewport bounds where available.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPlaces([FromQuery] string? q, CancellationToken cancellationToken = default)
    {
        if (q != null && q.Length > 200)
        {
            return BadRequest(new ApiErrorResponse(
                "INVALID_QUERY_LENGTH",
                "Search query exceeds maximum allowed length of 200 characters.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        var results = await _locationDiscoveryService.SearchPlacesAsync(q ?? string.Empty, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Reverse geocodes coordinates to a readable human location.
    /// </summary>
    [HttpGet("reverse")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReverseGeocode(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken = default)
    {
        if (!lat.HasValue || !lng.HasValue)
        {
            return BadRequest(new ApiErrorResponse(
                "MISSING_COORDINATES",
                "Both latitude and longitude are required for reverse geocoding.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        if (lat.Value < -90.0 || lat.Value > 90.0 || lng.Value < -180.0 || lng.Value > 180.0)
        {
            return BadRequest(new ApiErrorResponse(
                "INVALID_COORDINATES",
                "Latitude must be between -90 and +90, and longitude between -180 and +180.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        var city = await _geocodingProvider.ReverseGeocodeAsync(lat.Value, lng.Value, cancellationToken);
        var places = await _locationDiscoveryService.SearchPlacesAsync(city ?? "Ahmedabad", cancellationToken);
        var placeMatch = places.FirstOrDefault(p => p.Type == LocationType.City)
            ?? new Bookline.Application.Discovery.Dtos.PlaceSuggestionDto(
                PlaceId: $"rev-{lat.Value:F4}-{lng.Value:F4}",
                DisplayName: $"{city ?? "Ahmedabad"}, India",
                City: city ?? "Ahmedabad",
                Area: null,
                PostalCode: null,
                State: "Gujarat",
                Country: "India",
                Latitude: lat.Value,
                Longitude: lng.Value,
                Type: Bookline.Application.Discovery.Dtos.LocationType.City,
                ViewportBounds: null,
                SuggestedRadiusMeters: 15000.0
            );

        return Ok(placeMatch);
    }
}
