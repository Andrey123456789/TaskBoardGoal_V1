namespace TaskBoard.Domain.Enums;

/// <summary>
/// Outcome of a requested task status transition.
/// </summary>
public enum TaskTransitionOutcome
{
    Transitioned,

    /// <summary>The requested status is not the immediately following workflow state.</summary>
    NotAllowed,

    /// <summary>Starting work requires the task to be assigned to an active ordinary user.</summary>
    ActiveAssigneeRequired,
}
