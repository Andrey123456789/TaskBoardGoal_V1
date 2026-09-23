namespace TaskBoard.Domain.Enums;

/// <summary>
/// Outcome of an ordinary task update (title, description, assignee).
/// </summary>
public enum TaskUpdateOutcome
{
    Updated,

    /// <summary>The task is Completed or Closed; its core data is read-only.</summary>
    TaskLocked,

    /// <summary>An InProgress task cannot be manually unassigned.</summary>
    AssigneeRequired,

    /// <summary>The system Deleted User cannot be assigned manually.</summary>
    DeletedUserNotAssignable,
}
