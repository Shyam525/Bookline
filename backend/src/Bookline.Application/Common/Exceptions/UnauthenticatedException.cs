namespace Bookline.Application.Common.Exceptions;

/// <summary>
/// Thrown when an unauthenticated request attempts to access a protected operation (Section 82).
/// Maps to HTTP 401 Unauthorized with code UNAUTHENTICATED.
/// </summary>
public class UnauthenticatedException : Exception
{
    public string Code { get; }

    public UnauthenticatedException(
        string message = "Authentication is required to access this resource.",
        string code = "UNAUTHENTICATED")
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "UNAUTHENTICATED" : code;
    }
}
