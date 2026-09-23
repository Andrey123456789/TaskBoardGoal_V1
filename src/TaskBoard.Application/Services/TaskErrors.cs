using TaskBoard.Application.Results;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Services;

public static class TaskErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("task.not_found", $"Task '{id}' was not found.");

    public static Error ProjectNotFound(Guid projectId) =>
        Error.Unprocessable("task.project_not_found", $"Project '{projectId}' does not exist.");

    public static Error AssigneeNotFound(Guid assigneeId) =>
        Error.Unprocessable("task.assignee_not_found", $"User '{assigneeId}' does not exist.");

    public static readonly Error DeletedUserNotAssignable =
        Error.Unprocessable("task.deleted_user_not_assignable", "The Deleted User cannot be assigned to a task.");

    public static readonly Error AssigneeRequired =
        Error.Unprocessable("task.assignee_required", "A task that is in progress cannot be unassigned.");

    public static readonly Error ActiveAssigneeRequired =
        Error.Unprocessable(
            "task.active_assignee_required",
            "A task can be started only when it is assigned to an active user.");

    public static readonly Error SelfRelation =
        Error.Unprocessable("task.self_relation", "A task cannot be related to itself.");

    public static Error RelatedTasksNotFound(IEnumerable<Guid> missingIds) =>
        Error.Unprocessable(
            "task.related_tasks_not_found",
            $"Related tasks do not exist: {string.Join(", ", missingIds)}.");

    public static Error Locked(TaskItemStatus status) =>
        Error.Conflict("task.locked", $"A {status} task cannot be edited.");

    public static Error DeletionNotAllowed(TaskItemStatus status) =>
        Error.Conflict("task.deletion_not_allowed", $"A {status} task cannot be deleted.");

    public static Error InvalidTransition(TaskItemStatus from, TaskItemStatus to) =>
        Error.Conflict(
            "task.invalid_transition",
            $"A task cannot move from {from} to {to}.");
}
