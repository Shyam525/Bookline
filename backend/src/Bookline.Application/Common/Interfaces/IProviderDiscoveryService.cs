using Bookline.Application.Discovery.Dtos;

namespace Bookline.Application.Common.Interfaces;

/// <summary>
/// Phase 5: Provider Discovery Query Engine.
/// Authoritative application service for discovering public provider locations,
/// evaluating geographic proximity, applying deterministic ranking, and paginating results.
/// </summary>
public interface IProviderDiscoveryService
{
    /// <summary>
    /// Executes a provider discovery search against the database.
    /// Returns provider location results (not merely unlocated business headers).
    /// </summary>
    Task<DiscoveryResultResponse> DiscoverProvidersAsync(DiscoveryQuery query, CancellationToken cancellationToken = default);
}
