namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal static class Collations
{
    /// <summary>
    /// Explicit case-insensitive collation for columns whose uniqueness must ignore letter case,
    /// independent of the server or database default collation.
    /// </summary>
    public const string CaseInsensitive = "SQL_Latin1_General_CP1_CI_AS";
}
