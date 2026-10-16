namespace Bookline.Application.Common.Exceptions;

/// <summary>
/// Thrown when an internal component, job, or external integration is temporarily unavailable (Section 82).
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public class ServiceUnavailableException : Exception
{
    public string Code { get; }

    public ServiceUnavailableException(
        string message = "The service is temporarily unavailable. Please retry in a few moments.",
        string code = "SERVICE_UNAVAILABLE")
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "SERVICE_UNAVAILABLE" : code;
    }
}
