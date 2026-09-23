using System.Net.Mail;

namespace TaskBoard.Application.Validation;

internal static class EmailAddress
{
    /// <summary>
    /// Accepts a bare address (no display name) whose domain contains at least one dot,
    /// for example <c>jane.doe@example.com</c>.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();

        if (!MailAddress.TryCreate(candidate, out var address) ||
            !string.Equals(address.Address, candidate, StringComparison.Ordinal))
        {
            return false;
        }

        var host = address.Host;

        return host.Contains('.') &&
            !host.StartsWith('.') &&
            !host.EndsWith('.') &&
            !host.Contains("..", StringComparison.Ordinal);
    }
}
