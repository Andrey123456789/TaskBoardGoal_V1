using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Tasks;

[TestFixture]
public sealed class RelatedTasksApiTests : ApiTestBase
{
    private Guid _projectId;

    [SetUp]
    public async Task CreateSharedProjectAsync()
    {
        _projectId = (await CreateProjectAsync("Related tasks")).Id;
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithSeveralIds_AddsAllInOneRequest()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);
        var c = await CreateTaskAsync(_projectId);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id, c.Id), HttpStatusCode.NoContent);

        Assert.That(await RelatedIdsAsync(task.Id), Is.EquivalentTo(new[] { a.Id, b.Id, c.Id }));
    }

    [Test]
    public async Task ReplaceRelatedTasks_RepeatedIdenticalRequest_KeepsSameFinalState()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);

        var relatedIds = await RelatedIdsAsync(task.Id);
        var edgeCount = await EdgeCountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(relatedIds, Is.EquivalentTo(new[] { a.Id, b.Id }));
            Assert.That(edgeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithOneIdRemoved_RemovesThatRelationshipOnBothSides()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id), HttpStatusCode.NoContent);

        Assert.That(await RelatedIdsAsync(task.Id), Is.EqualTo(new[] { a.Id }));
        Assert.That(await RelatedIdsAsync(b.Id), Is.Empty);
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithAnotherIdAdded_AddsItAndKeepsExisting()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id), HttpStatusCode.NoContent);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);

        Assert.That(await RelatedIdsAsync(task.Id), Is.EquivalentTo(new[] { a.Id, b.Id }));
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithDuplicateIds_CreatesOneRelationship()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, a.Id, a.Id), HttpStatusCode.NoContent);

        var relatedIds = await RelatedIdsAsync(task.Id);
        var edgeCount = await EdgeCountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(relatedIds, Is.EqualTo(new[] { a.Id }));
            Assert.That(edgeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ReplaceRelatedTasks_FromBothSides_StoresSingleSymmetricRelationship()
    {
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(a.Id, b.Id), HttpStatusCode.NoContent);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(b.Id, a.Id), HttpStatusCode.NoContent);

        var relatedToA = await RelatedIdsAsync(a.Id);
        var relatedToB = await RelatedIdsAsync(b.Id);
        var edgeCount = await EdgeCountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(relatedToA, Is.EqualTo(new[] { b.Id }));
            Assert.That(relatedToB, Is.EqualTo(new[] { a.Id }));
            Assert.That(edgeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithSelfReference_Returns422()
    {
        var task = await CreateTaskAsync(_projectId);

        await AssertProblemAsync(
            await ReplaceRelatedTasksAsync(task.Id, task.Id),
            HttpStatusCode.UnprocessableEntity,
            "task.self_relation");
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithNonExistentTask_Returns422AndKeepsCurrentSet()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id), HttpStatusCode.NoContent);

        var response = await ReplaceRelatedTasksAsync(task.Id, b.Id, Guid.NewGuid());

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.related_tasks_not_found");
        Assert.That(await RelatedIdsAsync(task.Id), Is.EqualTo(new[] { a.Id }));
    }

    [Test]
    public async Task RelatedTaskDetails_AreSymmetricAndIdentifyTheOtherTask()
    {
        var otherProject = await CreateProjectAsync("Other project");
        var a = await CreateTaskAsync(_projectId, title: "Task A");
        var b = await CreateTaskAsync(otherProject.Id, title: "Task B");

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(a.Id, b.Id), HttpStatusCode.NoContent);

        var fromA = (await GetTaskAsync(a.Id)).RelatedTasks.Single();
        var fromB = (await GetTaskAsync(b.Id)).RelatedTasks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(fromA.Id, Is.EqualTo(b.Id));
            Assert.That(fromA.Title, Is.EqualTo("Task B"));
            Assert.That(fromA.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(fromA.Project.Name, Is.EqualTo("Other project"));
            Assert.That(fromB.Id, Is.EqualTo(a.Id));
            Assert.That(fromB.Title, Is.EqualTo("Task A"));
        });
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithEmptyArray_RemovesAllRelationships()
    {
        var task = await CreateTaskAsync(_projectId);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id), HttpStatusCode.NoContent);

        var relatedToTask = await RelatedIdsAsync(task.Id);
        var relatedToA = await RelatedIdsAsync(a.Id);
        var edgeCount = await EdgeCountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(relatedToTask, Is.Empty);
            Assert.That(relatedToA, Is.Empty);
            Assert.That(edgeCount, Is.Zero);
        });
    }

    [TestCase(TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Closed)]
    public async Task ReplaceRelatedTasks_OnCompletedOrClosedTask_IsAllowedAndKeepsCoreData(TaskItemStatus status)
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(_projectId, user.Id, status);
        var a = await CreateTaskAsync(_projectId);
        var b = await CreateTaskAsync(_projectId);

        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, a.Id, b.Id), HttpStatusCode.NoContent);
        await AssertStatusAsync(await ReplaceRelatedTasksAsync(task.Id, b.Id), HttpStatusCode.NoContent);

        var after = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(after.RelatedTasks.Select(x => x.Id), Is.EqualTo(new[] { b.Id }));
            Assert.That(after.Status, Is.EqualTo(status));
            Assert.That(after.UpdatedAt, Is.EqualTo(task.UpdatedAt));
        });
    }

    [Test]
    public async Task ReplaceRelatedTasks_WithoutRelatedTaskIds_Returns400()
    {
        var task = await CreateTaskAsync(_projectId);

        var response = await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/related-tasks", new { }, Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ReplaceRelatedTasks_UnknownTask_Returns404()
    {
        var other = await CreateTaskAsync(_projectId);

        await AssertProblemAsync(
            await ReplaceRelatedTasksAsync(Guid.NewGuid(), other.Id),
            HttpStatusCode.NotFound,
            "task.not_found");
    }

    [Test]
    public async Task CreateFollowUpTask_FromCompletedTask_RelatesItWithoutChangingSource()
    {
        var user = await CreateUserAsync();
        var source = await CreateTaskInStatusAsync(_projectId, user.Id, TaskItemStatus.Completed, "Original work");

        var followUp = await CreateTaskAsync(_projectId, user.Id, "Follow-up work", relatedTaskIds: [source.Id]);

        var sourceAfter = await GetTaskAsync(source.Id);
        Assert.Multiple(() =>
        {
            Assert.That(followUp.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(followUp.RelatedTasks.Select(x => x.Id), Is.EqualTo(new[] { source.Id }));
            Assert.That(sourceAfter.RelatedTasks.Select(x => x.Id), Is.EqualTo(new[] { followUp.Id }));
            Assert.That(sourceAfter.Status, Is.EqualTo(TaskItemStatus.Completed));
            Assert.That(sourceAfter.Title, Is.EqualTo(source.Title));
            Assert.That(sourceAfter.UpdatedAt, Is.EqualTo(source.UpdatedAt));
        });
    }

    private async Task<IReadOnlyList<Guid>> RelatedIdsAsync(Guid taskId) =>
        (await GetTaskAsync(taskId)).RelatedTasks.Select(x => x.Id).ToList();

    private static Task<int> EdgeCountAsync() =>
        Factory.QueryDatabaseAsync(db => db.TaskRelations.CountAsync());
}
