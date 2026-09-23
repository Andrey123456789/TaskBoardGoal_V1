namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// The current assignee of a task. An unassigned task has no assignee at all, which keeps it
/// distinguishable from a task assigned to the system Deleted User (<see cref="IsDeletedUser"/>).
/// </summary>
public sealed record AssigneeDto(
    Guid Id,
    string Name,
    bool IsDeletedUser);
