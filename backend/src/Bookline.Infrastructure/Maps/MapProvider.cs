using Bookline.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Bookline.Infrastructure.Maps;

public class MapProvider : IMapProvider
{
    private readonly IConfiguration _configuration;

    public MapProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ProviderName => _configuration["Maps:Provider"] ?? "OpenStreetMap";

    public bool IsConfigured => true;

    public string GetTileUrlTemplate()
    {
        // Dark theme tile URL (CartoDB Dark Matter / Stadia / OSM default)
        var customTile = _configuration["Maps:TileUrl"];
        if (!string.IsNullOrEmpty(customTile)) return customTile;

        return "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png";
    }

    public string? GetClientApiKey()
    {
        return _configuration["Maps:ApiKey"];
    }
}
