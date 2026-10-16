namespace Bookline.Application.Common.Exceptions;

/// <summary>
/// Thrown when an operational, state, inventory, or resource conflict occurs (Section 82).
/// Maps to HTTP 409 Conflict with a machine-readable code.
/// </summary>
public class ConflictException : Exception
{
    public string Code { get; }
    public IDictionary<string, object?>? Extensions { get; }

    public ConflictException(string code, string message, IDictionary<string, object?>? extensions = null)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "CONFLICT" : code;
        Extensions = extensions;
    }

    public static ConflictException Inventory(string message, IDictionary<string, object?>? extensions = null) =>
        new("INVENTORY_CONFLICT", message, extensions);

    public static ConflictException State(string message, IDictionary<string, object?>? extensions = null) =>
        new("STATE_CONFLICT", message, extensions);

    public static ConflictException Slot(string message, IDictionary<string, object?>? extensions = null) =>
        new("SLOT_UNAVAILABLE", message, extensions);
}
