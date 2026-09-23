namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// The complete desired set of related task IDs. An empty collection removes every relationship.
/// </summary>
public sealed record ReplaceRelatedTasksRequest(
    IReadOnlyList<Guid> RelatedTaskIds);
