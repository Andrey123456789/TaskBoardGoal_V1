using System.Net;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Users;

[TestFixture]
public sealed class UserDeletionWorkflowTests : ApiTestBase
{
    private static readonly AssigneeDto DeletedUserAssignee =
        new(User.DeletedUserId, User.DeletedUserName, IsDeletedUser: true);

    [Test]
    public async Task DeleteUser_WithActiveAndHistoricalTasks_ReassignsEveryTaskToDeletedUser()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var john = await CreateUserAsync();
        var created = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.Created);
        var inProgress = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.InProgress);
        var completed = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.Completed);
        var closed = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.Closed);
        var johnsTask = await CreateTaskAsync(project.Id, john.Id);

        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        foreach (var before in new[] { created, inProgress, completed, closed })
        {
            var after = await GetTaskAsync(before.Id);

            Assert.Multiple(() =>
            {
                Assert.That(after.Assignee, Is.EqualTo(DeletedUserAssignee));
                Assert.That(after.Status, Is.EqualTo(before.Status));
                Assert.That(after.Title, Is.EqualTo(before.Title));
                Assert.That(after.UpdatedAt, Is.EqualTo(before.UpdatedAt));
            });
        }

        Assert.That((await GetTaskAsync(johnsTask.Id)).Assignee?.Id, Is.EqualTo(john.Id));

        var deletedUsersTasks = await ListTasksAsync($"?assigneeId={User.DeletedUserId}");
        Assert.That(
            deletedUsersTasks.Select(x => x.Id),
            Is.EquivalentTo(new[] { created.Id, inProgress.Id, completed.Id, closed.Id }));

        var users = await ListUsersAsync();
        Assert.That(users.Select(x => x.Id), Is.EqualTo(new[] { john.Id }));
    }

    [Test]
    public async Task CreatedTaskOfDeletedAssignee_CannotStartUntilAssignedToActiveUser()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var john = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, jane.Id);

        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        var afterDeletion = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(afterDeletion.Assignee, Is.EqualTo(DeletedUserAssignee));
            Assert.That(afterDeletion.Status, Is.EqualTo(TaskItemStatus.Created));
        });

        await AssertProblemAsync(
            await ChangeStatusAsync(task.Id, "InProgress"),
            HttpStatusCode.UnprocessableEntity,
            "task.active_assignee_required");

        await AssertStatusAsync(await UpdateTaskAsync(task.Id, task.Title, null, john.Id), HttpStatusCode.NoContent);
        await AssertStatusAsync(await ChangeStatusAsync(task.Id, "InProgress"), HttpStatusCode.NoContent);

        var started = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(started.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(started.Assignee?.Id, Is.EqualTo(john.Id));
        });
    }

    [Test]
    public async Task InProgressTaskOfDeletedAssignee_StaysInProgressAndCanBeReassigned()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var john = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.InProgress);

        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        var afterDeletion = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(afterDeletion.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(afterDeletion.Assignee, Is.EqualTo(DeletedUserAssignee));
        });

        await AssertStatusAsync(await UpdateTaskAsync(task.Id, task.Title, null, john.Id), HttpStatusCode.NoContent);

        var reassigned = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(reassigned.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(reassigned.Assignee?.Id, Is.EqualTo(john.Id));
        });
    }

    [Test]
    public async Task InProgressTaskOfDeletedAssignee_CannotBeManuallyUnassigned()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.InProgress);
        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        await AssertProblemAsync(
            await UpdateTaskAsync(task.Id, task.Title, null, null),
            HttpStatusCode.UnprocessableEntity,
            "task.assignee_required");
    }

    [Test]
    public async Task CompletedTaskOfDeletedAssignee_IsNotReopenedOrOtherwiseModified()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.Completed);

        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        var after = await GetTaskAsync(task.Id);
        Assert.That(after, Is.EqualTo(task with { Assignee = DeletedUserAssignee, RelatedTasks = after.RelatedTasks }));
        Assert.That(after.RelatedTasks, Is.Empty);
    }

    [Test]
    public async Task TaskList_DistinguishesUnassignedFromDeletedUser()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var unassigned = await CreateTaskAsync(project.Id);
        var orphaned = await CreateTaskAsync(project.Id, jane.Id);
        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{jane.Id}"), HttpStatusCode.NoContent);

        var tasks = await ListTasksAsync();

        Assert.Multiple(() =>
        {
            Assert.That(tasks.Single(x => x.Id == unassigned.Id).Assignee, Is.Null);
            Assert.That(tasks.Single(x => x.Id == orphaned.Id).Assignee, Is.EqualTo(DeletedUserAssignee));
        });
    }
}
