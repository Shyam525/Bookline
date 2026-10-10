using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker job expiring stale team and organization invitations adhering to Section 122.
/// Removes unaccepted invitations past their expiration deadline.
/// </summary>
public class InvitationCleanupJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InvitationCleanupJob> _logger;

    public InvitationCleanupJob(IApplicationDbContext context, ILogger<InvitationCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredInvitations = await _context.Invitations
            .IgnoreQueryFilters()
            .Where(inv => !inv.IsAccepted && inv.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (!expiredInvitations.Any()) return 0;

        _logger.LogInformation("Removing {Count} stale invitations past expiration deadline...", expiredInvitations.Count);

        _context.Invitations.RemoveRange(expiredInvitations);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredInvitations.Count;
    }
}
