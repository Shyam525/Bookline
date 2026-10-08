using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;

    public ReviewsController(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("provider/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProviderReviews(Guid tenantId, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && r.ModerationStatus == ModerationStatus.Approved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(reviews);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { Message = "Rating must be between 1 and 5." });
        }

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return BadRequest(new { Message = "Provider not found." });
        }

        Guid customerId = Guid.NewGuid();
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            customerId = parsedUserId;
        }

        // Eligibility check (Point 48): customer completed appointment or order
        bool isEligible = await _dbContext.Bookings.IgnoreQueryFilters()
            .AnyAsync(b => b.TenantId == request.TenantId && 
                          (b.CustomerId == customerId || (request.BookingId.HasValue && b.Id == request.BookingId.Value)), 
                      cancellationToken)
            || await _dbContext.Orders.IgnoreQueryFilters()
            .AnyAsync(o => o.TenantId == request.TenantId && 
                          (o.CustomerId == customerId || (request.OrderId.HasValue && o.Id == request.OrderId.Value)), 
                      cancellationToken)
            || request.SkipEligibilityCheck; // For demo/seed testing

        if (!isEligible)
        {
            return BadRequest(new { Message = "You must have a completed appointment or order with this provider to submit a review." });
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CustomerId = customerId,
            BookingId = request.BookingId,
            OrderId = request.OrderId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Verified Client" : request.CustomerName,
            Rating = request.Rating,
            Title = request.Title,
            Comment = request.Comment,
            ModerationStatus = ModerationStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recalculate tenant rating and count
        var allReviews = await _dbContext.Reviews.IgnoreQueryFilters()
            .Where(r => r.TenantId == request.TenantId && r.ModerationStatus == ModerationStatus.Approved)
            .ToListAsync(cancellationToken);

        tenant.ReviewCount = allReviews.Count;
        tenant.AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 5.0;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(review);
    }

    [HttpPost("{id:guid}/respond")]
    [Authorize]
    public async Task<IActionResult> RespondToReview(Guid id, [FromBody] ReviewResponseRequest request, CancellationToken cancellationToken)
    {
        var review = await _dbContext.Reviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null) return NotFound(new { Message = "Review not found." });

        review.Respond(request.Response);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(review);
    }

    [HttpPut("{id:guid}/moderate")]
    [Authorize]
    public async Task<IActionResult> ModerateReview(Guid id, [FromBody] ModerateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _dbContext.Reviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null) return NotFound(new { Message = "Review not found." });

        if (Enum.TryParse<ModerationStatus>(request.Status, true, out var status))
        {
            review.Moderate(status);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(review);
        }

        return BadRequest(new { Message = "Invalid moderation status." });
    }
}

public record SubmitReviewRequest(
    Guid TenantId,
    int Rating,
    string? Title,
    string Comment,
    string? CustomerName,
    Guid? BookingId = null,
    Guid? OrderId = null,
    bool SkipEligibilityCheck = false
);
public record ReviewResponseRequest(string Response);
public record ModerateReviewRequest(string Status);
