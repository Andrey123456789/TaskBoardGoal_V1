using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Tests;

[TestFixture]
public sealed class TaskItemTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddHours(1);
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    [Test]
    public void Create_WithValidInput_StartsAsCreatedWithMatchingTimestamps()
    {
        var task = TaskItem.Create("Title", "Description", ProjectId, assigneeId: null, CreatedAt);

        Assert.Multiple(() =>
        {
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(task.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(task.ProjectId, Is.EqualTo(ProjectId));
            Assert.That(task.AssigneeId, Is.Null);
            Assert.That(task.CreatedAt, Is.EqualTo(CreatedAt));
            Assert.That(task.UpdatedAt, Is.EqualTo(CreatedAt));
        });
    }

    [Test]
    public void Create_AssignedToDeletedUser_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TaskItem.Create("Title", null, ProjectId, User.DeletedUserId, CreatedAt));
    }

    [TestCase(TaskItemStatus.Created, TaskItemStatus.InProgress, true)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Completed, true)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.Closed, true)]
    [TestCase(TaskItemStatus.Created, TaskItemStatus.Created, false)]
    [TestCase(TaskItemStatus.Created, TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Created, TaskItemStatus.Closed, false)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Created, false)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.InProgress, false)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Closed, false)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.Created, false)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.InProgress, false)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Created, false)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.InProgress, false)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Closed, TaskItemStatus.Closed, false)]
    public void TransitionTo_FollowsStrictlyLinearWorkflow(
        TaskItemStatus current,
        TaskItemStatus target,
        bool allowed)
    {
        var task = CreateTaskIn(current, UserId);

        var outcome = task.TransitionTo(target, Later);

        Assert.Multiple(() =>
        {
            Assert.That(
                outcome,
                Is.EqualTo(allowed ? TaskTransitionOutcome.Transitioned : TaskTransitionOutcome.NotAllowed));
            Assert.That(task.Status, Is.EqualTo(allowed ? target : current));
        });
    }

    [Test]
    public void TransitionTo_Allowed_UpdatesUpdatedAt()
    {
        var task = TaskItem.Create("Title", null, ProjectId, UserId, CreatedAt);

        task.TransitionTo(TaskItemStatus.InProgress, Later);

        Assert.That(task.UpdatedAt, Is.EqualTo(Later));
    }

    [Test]
    public void TransitionTo_StartUnassignedTask_RequiresActiveAssignee()
    {
        var task = TaskItem.Create("Title", null, ProjectId, assigneeId: null, CreatedAt);

        var outcome = task.TransitionTo(TaskItemStatus.InProgress, Later);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskTransitionOutcome.ActiveAssigneeRequired));
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(task.UpdatedAt, Is.EqualTo(CreatedAt));
        });
    }

    [Test]
    public void TransitionTo_StartTaskAssignedToDeletedUser_RequiresActiveAssignee()
    {
        var task = TaskItem.Create("Title", null, ProjectId, UserId, CreatedAt);
        task.ReassignToDeletedUser();

        var outcome = task.TransitionTo(TaskItemStatus.InProgress, Later);

        Assert.That(outcome, Is.EqualTo(TaskTransitionOutcome.ActiveAssigneeRequired));
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    public void UpdateDetails_EditableStatus_UpdatesCoreFields(TaskItemStatus status)
    {
        var task = CreateTaskIn(status, UserId);

        var outcome = task.UpdateDetails("New title", "New description", OtherUserId, Later);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.Updated));
            Assert.That(task.Title, Is.EqualTo("New title"));
            Assert.That(task.Description, Is.EqualTo("New description"));
            Assert.That(task.AssigneeId, Is.EqualTo(OtherUserId));
            Assert.That(task.UpdatedAt, Is.EqualTo(Later));
            Assert.That(task.Status, Is.EqualTo(status));
        });
    }

    [Test]
    public void UpdateDetails_CreatedTask_CanBeUnassigned()
    {
        var task = TaskItem.Create("Title", null, ProjectId, UserId, CreatedAt);

        var outcome = task.UpdateDetails("Title", null, assigneeId: null, Later);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.Updated));
            Assert.That(task.AssigneeId, Is.Null);
        });
    }

    [Test]
    public void UpdateDetails_InProgressTask_CannotBeUnassigned()
    {
        var task = CreateTaskIn(TaskItemStatus.InProgress, UserId);

        var outcome = task.UpdateDetails("Title", null, assigneeId: null, Later);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.AssigneeRequired));
            Assert.That(task.AssigneeId, Is.EqualTo(UserId));
        });
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    public void UpdateDetails_AssigningDeletedUser_IsRejected(TaskItemStatus status)
    {
        var task = CreateTaskIn(status, UserId);

        var outcome = task.UpdateDetails("Title", null, User.DeletedUserId, Later);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.DeletedUserNotAssignable));
            Assert.That(task.AssigneeId, Is.EqualTo(UserId));
        });
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public void UpdateDetails_LockedTask_IsRejectedWithoutChanges(TaskItemStatus status)
    {
        var task = CreateTaskIn(status, UserId);
        var updatedAt = task.UpdatedAt;

        var outcome = task.UpdateDetails("New title", "New description", OtherUserId, Later.AddDays(10));

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.TaskLocked));
            Assert.That(task.Title, Is.EqualTo("Title"));
            Assert.That(task.AssigneeId, Is.EqualTo(UserId));
            Assert.That(task.UpdatedAt, Is.EqualTo(updatedAt));
        });
    }

    [Test]
    public void UpdateDetails_WithoutChanges_KeepsUpdatedAt()
    {
        var task = TaskItem.Create("Title", "Description", ProjectId, UserId, CreatedAt);

        task.UpdateDetails("Title", "Description", UserId, Later);

        Assert.That(task.UpdatedAt, Is.EqualTo(CreatedAt));
    }

    [TestCase(TaskItemStatus.Created, true)]
    [TestCase(TaskItemStatus.InProgress, true)]
    [TestCase(TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Closed, false)]
    public void CanBeDeleted_OnlyBeforeCompletion(TaskItemStatus status, bool expected)
    {
        var task = CreateTaskIn(status, UserId);

        Assert.That(task.CanBeDeleted, Is.EqualTo(expected));
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public void ReassignToDeletedUser_AnyStatus_KeepsStatusAndUpdatedAt(TaskItemStatus status)
    {
        var task = CreateTaskIn(status, UserId);
        var updatedAt = task.UpdatedAt;

        task.ReassignToDeletedUser();

        Assert.Multiple(() =>
        {
            Assert.That(task.AssigneeId, Is.EqualTo(User.DeletedUserId));
            Assert.That(task.Status, Is.EqualTo(status));
            Assert.That(task.UpdatedAt, Is.EqualTo(updatedAt));
            Assert.That(task.HasActiveAssignee, Is.False);
        });
    }

    [Test]
    public void UpdateDetails_InProgressTaskAssignedToDeletedUser_CanBeReassignedToActiveUser()
    {
        var task = CreateTaskIn(TaskItemStatus.InProgress, UserId);
        task.ReassignToDeletedUser();

        var outcome = task.UpdateDetails("Title", null, OtherUserId, Later.AddDays(5));

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(TaskUpdateOutcome.Updated));
            Assert.That(task.AssigneeId, Is.EqualTo(OtherUserId));
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.InProgress));
        });
    }

    private static TaskItem CreateTaskIn(TaskItemStatus status, Guid assigneeId)
    {
        var task = TaskItem.Create("Title", null, ProjectId, assigneeId, CreatedAt);
        var time = CreatedAt;

        while (task.Status != status)
        {
            time = time.AddMinutes(1);
            task.TransitionTo(task.NextStatus!.Value, time);
        }

        return task;
    }
}
