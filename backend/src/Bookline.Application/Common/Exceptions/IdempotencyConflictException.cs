namespace Bookline.Application.Common.Exceptions;

public class IdempotencyConflictException : Exception
{
    public string IdempotencyKey { get; }

    public IdempotencyConflictException(string idempotencyKey, string message = "A concurrent request with the same idempotency key is currently processing.")
        : base(message)
    {
        IdempotencyKey = idempotencyKey;
    }
}
