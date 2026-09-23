using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

/// <summary>
/// A unit of work within a project. Exposed publicly as the <c>Task</c> resource.
/// </summary>
public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private TaskItem()
    {
    }

    private TaskItem(
        Guid id,
        string title,
        string? description,
        Guid projectId,
        Guid? assigneeId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        ProjectId = projectId;
        AssigneeId = assigneeId;
        Status = TaskItemStatus.Created;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Set only on creation; a task never moves to another project.</summary>
    public Guid ProjectId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Project? Project { get; private set; }

    public User? Assignee { get; private set; }

    /// <summary>Completed and Closed tasks have read-only core data.</summary>
    public bool IsLocked => Status is TaskItemStatus.Completed or TaskItemStatus.Closed;

    public bool CanBeDeleted => !IsLocked;

    public bool HasActiveAssignee => AssigneeId is { } assigneeId && assigneeId != User.DeletedUserId;

    /// <summary>The only status this task may move to, or <c>null</c> when the status is terminal.</summary>
    public TaskItemStatus? NextStatus => Status switch
    {
        TaskItemStatus.Created => TaskItemStatus.InProgress,
        TaskItemStatus.InProgress => TaskItemStatus.Completed,
        TaskItemStatus.Completed => TaskItemStatus.Closed,
        _ => null,
    };

    /// <summary>
    /// Creates a task in the <see cref="TaskItemStatus.Created"/> state. The caller is responsible for
    /// verifying that the project and the assignee exist.
    /// </summary>
    public static TaskItem Create(
        string title,
        string? description,
        Guid projectId,
        Guid? assigneeId,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("A task must belong to a project.", nameof(projectId));
        }

        if (assigneeId == User.DeletedUserId)
        {
            throw new ArgumentException("The Deleted User cannot be assigned manually.", nameof(assigneeId));
        }

        return new TaskItem(Guid.NewGuid(), title, description, projectId, assigneeId, createdAt);
    }

    /// <summary>
    /// Applies an ordinary edit of the core fields. The caller is responsible for verifying that a
    /// supplied assignee is an existing ordinary user.
    /// </summary>
    public TaskUpdateOutcome UpdateDetails(
        string title,
        string? description,
        Guid? assigneeId,
        DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (IsLocked)
        {
            return TaskUpdateOutcome.TaskLocked;
        }

        if (assigneeId == User.DeletedUserId)
        {
            return TaskUpdateOutcome.DeletedUserNotAssignable;
        }

        if (Status == TaskItemStatus.InProgress && assigneeId is null)
        {
            return TaskUpdateOutcome.AssigneeRequired;
        }

        if (Title == title && Description == description && AssigneeId == assigneeId)
        {
            return TaskUpdateOutcome.Updated;
        }

        Title = title;
        Description = description;
        AssigneeId = assigneeId;
        UpdatedAt = updatedAt;

        return TaskUpdateOutcome.Updated;
    }

    public TaskTransitionOutcome TransitionTo(TaskItemStatus target, DateTimeOffset updatedAt)
    {
        if (NextStatus != target)
        {
            return TaskTransitionOutcome.NotAllowed;
        }

        if (target == TaskItemStatus.InProgress && !HasActiveAssignee)
        {
            return TaskTransitionOutcome.ActiveAssigneeRequired;
        }

        Status = target;
        UpdatedAt = updatedAt;

        return TaskTransitionOutcome.Transitioned;
    }

    /// <summary>
    /// System operation performed when the current assignee is deleted. It is allowed in every
    /// status and leaves the status, the core data and <see cref="UpdatedAt"/> untouched.
    /// </summary>
    public void ReassignToDeletedUser()
    {
        if (AssigneeId is null)
        {
            throw new InvalidOperationException("An unassigned task has no assignee to replace.");
        }

        AssigneeId = User.DeletedUserId;
    }
}
