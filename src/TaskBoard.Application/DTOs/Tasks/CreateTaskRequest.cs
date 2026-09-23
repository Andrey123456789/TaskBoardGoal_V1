namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// Request body for creating a task. A new task always starts as Created; the status cannot be set.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Guid ProjectId,
    Guid? AssigneeId,
    IReadOnlyList<Guid>? RelatedTaskIds);
