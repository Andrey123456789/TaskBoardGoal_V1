namespace TaskBoard.Application.Services;

internal static class InputText
{
    /// <summary>Trims a required value that has already passed validation.</summary>
    public static string Required(string value) => value.Trim();

    /// <summary>Trims an optional value; blank input is stored as <c>null</c>.</summary>
    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
