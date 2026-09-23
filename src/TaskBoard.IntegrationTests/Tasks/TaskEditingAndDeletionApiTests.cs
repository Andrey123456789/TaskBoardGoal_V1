using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Tasks;

[TestFixture]
public sealed class TaskEditingAndDeletionApiTests : ApiTestBase
{
    [Test]
    public async Task UpdateTask_CreatedTask_UpdatesCoreFields()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync("Jane");
        var task = await CreateTaskAsync(project.Id, title: "Old", description: "Old description");

        await AssertStatusAsync(
            await UpdateTaskAsync(task.Id, "New", "New description", jane.Id),
            HttpStatusCode.NoContent);

        var updated = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Title, Is.EqualTo("New"));
            Assert.That(updated.Description, Is.EqualTo("New description"));
            Assert.That(updated.Assignee?.Id, Is.EqualTo(jane.Id));
            Assert.That(updated.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(updated.Project.Id, Is.EqualTo(project.Id));
            Assert.That(updated.UpdatedAt, Is.GreaterThan(task.UpdatedAt));
        });
    }

    [Test]
    public async Task UpdateTask_CreatedTask_CanBeUnassigned()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, jane.Id);

        await AssertStatusAsync(await UpdateTaskAsync(task.Id, task.Title, null, null), HttpStatusCode.NoContent);

        Assert.That((await GetTaskAsync(task.Id)).Assignee, Is.Null);
    }

    [Test]
    public async Task UpdateTask_WithProjectId_Returns400AndKeepsProject()
    {
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new { title = "New", projectId = otherProject.Id },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That((await GetTaskAsync(task.Id)).Project.Id, Is.EqualTo(project.Id));
    }

    [Test]
    public async Task UpdateTask_WithStatus_Returns400AndKeepsStatus()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new { title = "New", status = "Completed" },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.Created));
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    public async Task UpdateTask_AssigningDeletedUser_Returns422(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, status);

        var response = await UpdateTaskAsync(task.Id, task.Title, null, User.DeletedUserId);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.deleted_user_not_assignable");
        Assert.That((await GetTaskAsync(task.Id)).Assignee?.Id, Is.EqualTo(jane.Id));
    }

    [Test]
    public async Task UpdateTask_WithNonExistentAssignee_Returns422()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(
            await UpdateTaskAsync(task.Id, "Title", null, Guid.NewGuid()),
            HttpStatusCode.UnprocessableEntity,
            "task.assignee_not_found");
    }

    [Test]
    public async Task UpdateTask_UnknownTask_Returns404()
    {
        await AssertProblemAsync(
            await UpdateTaskAsync(Guid.NewGuid(), "Title", null, null),
            HttpStatusCode.NotFound,
            "task.not_found");
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task UpdateTask_CompletedOrClosed_Returns409AndKeepsData(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var john = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, status, title: "Original");

        var response = await UpdateTaskAsync(task.Id, "Changed", "Changed", john.Id);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "task.locked");
        var unchanged = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Title, Is.EqualTo("Original"));
            Assert.That(unchanged.Assignee?.Id, Is.EqualTo(jane.Id));
            Assert.That(unchanged.UpdatedAt, Is.EqualTo(task.UpdatedAt));
        });
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task DeleteTask_CompletedOrClosed_Returns409AndKeepsTask(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);

        var response = await Client.DeleteAsync($"/api/tasks/{task.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "task.deletion_not_allowed");
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(status));
    }

    [TestCase(TaskItemStatus.Created)]
    [TestCase(TaskItemStatus.InProgress)]
    public async Task DeleteTask_CreatedOrInProgress_Returns204AndRemovesItsRelationships(TaskItemStatus status)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var related = await CreateTaskAsync(project.Id);
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, status);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, related.Id), HttpStatusCode.NoContent);

        await AssertStatusAsync(await Client.DeleteAsync($"/api/tasks/{task.Id}"), HttpStatusCode.NoContent);

        await AssertProblemAsync(await Client.GetAsync($"/api/tasks/{task.Id}"), HttpStatusCode.NotFound);
        Assert.That((await GetTaskAsync(related.Id)).RelatedTasks, Is.Empty);

        var remainingEdges = await Factory.QueryDatabaseAsync(db =>
            db.TaskRelations.CountAsync(x => x.FirstTaskId == task.Id || x.SecondTaskId == task.Id));
        Assert.That(remainingEdges, Is.Zero);
    }

    [Test]
    public async Task DeleteTask_UnknownTask_Returns404()
    {
        await AssertProblemAsync(
            await Client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}"),
            HttpStatusCode.NotFound,
            "task.not_found");
    }
}
