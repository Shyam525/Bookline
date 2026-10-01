namespace Bookline.Infrastructure.Jobs;

using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class OutboxProcessorJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(IApplicationDbContext context, ILogger<OutboxProcessorJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await _context.OutboxMessages
            .IgnoreQueryFilters()
            .Where(m => m.Status == "Pending" && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any())
        {
            return;
        }

        foreach (var msg in pendingMessages)
        {
            try
            {
                _logger.LogInformation("Processing Outbox Event [{Id}] of type {EventType} for Tenant {TenantId}",
                    msg.Id, msg.EventType, msg.TenantId);

                // Process notification side effect (e.g. Email dispatch / Audit logging)
                msg.Status = "Processed";
                msg.ProcessedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Outbox Event [{Id}]", msg.Id);
                msg.RetryCount++;
                msg.Error = ex.Message;
                if (msg.RetryCount >= 5)
                {
                    msg.Status = "Failed";
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
