namespace TaskBoard.Domain.Enums;

/// <summary>
/// Task workflow states. The workflow is strictly linear:
/// Created -> InProgress -> Completed -> Closed.
/// </summary>
public enum TaskItemStatus
{
    Created = 0,
    InProgress = 1,
    Completed = 2,
    Closed = 3,
}
