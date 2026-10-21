using System.Diagnostics.Metrics;

namespace Bookline.Infrastructure.Observability;

/// <summary>
/// Section 119: Low-cardinality operational metrics for rates, latencies, and business lifecycle outcomes.
/// Excludes high-cardinality PII such as raw user IDs, emails, GPS values, or arbitrary query text.
/// </summary>
public static class BooklineMetrics
{
    public const string MeterName = "Bookline.Observability";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    /// <summary>
    /// Total count of requests rejected by rate limiting policies.
    /// Labels: policy
    /// </summary>
    public static readonly Counter<long> RateLimitedRequests = Meter.CreateCounter<long>(
        "bookline_ratelimited_requests_total",
        description: "Count of requests rejected due to rate limiting policies");

    /// <summary>
    /// Total count of booking slot conflicts encountered.
    /// Labels: outcome
    /// </summary>
    public static readonly Counter<long> BookingConflicts = Meter.CreateCounter<long>(
        "bookline_booking_conflicts_total",
        description: "Count of slot booking conflicts encountered");

    /// <summary>
    /// Total count of transactional outbox events processed.
    /// Labels: event_type, outcome
    /// </summary>
    public static readonly Counter<long> OutboxEventsProcessed = Meter.CreateCounter<long>(
        "bookline_outbox_events_processed_total",
        description: "Count of transactional outbox events successfully processed");

    /// <summary>
    /// Total count of transactional outbox events that failed.
    /// Labels: event_type, error_category
    /// </summary>
    public static readonly Counter<long> OutboxEventsFailed = Meter.CreateCounter<long>(
        "bookline_outbox_events_failed_total",
        description: "Count of transactional outbox events that encountered failures");

    /// <summary>
    /// Total count of notification deliveries executed.
    /// Labels: channel, outcome
    /// </summary>
    public static readonly Counter<long> NotificationDeliveries = Meter.CreateCounter<long>(
        "bookline_notification_deliveries_total",
        description: "Count of customer/provider notification deliveries");

    /// <summary>
    /// Total count of incoming payment gateway webhooks processed.
    /// Labels: event_type, status
    /// </summary>
    public static readonly Counter<long> WebhookProcessed = Meter.CreateCounter<long>(
        "bookline_webhook_processed_total",
        description: "Count of payment gateway webhooks processed");

    /// <summary>
    /// Total count of health probe check failures.
    /// Labels: check_name
    /// </summary>
    public static readonly Counter<long> HealthCheckFailures = Meter.CreateCounter<long>(
        "bookline_healthcheck_failures_total",
        description: "Count of health check probe failures");

    /// <summary>
    /// Outbox batch processing duration histogram in milliseconds.
    /// </summary>
    public static readonly Histogram<double> OutboxBatchDurationMs = Meter.CreateHistogram<double>(
        "bookline_outbox_batch_duration_ms",
        unit: "ms",
        description: "Duration of transactional outbox batch processing cycles");
}
