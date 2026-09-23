using System.Net;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Tasks;

[TestFixture]
public sealed class TaskWorkflowApiTests : ApiTestBase
{
    [Test]
    public async Task Start_UnassignedTask_Returns422AndKeepsCreated()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await ChangeStatusAsync(task.Id, "InProgress");

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.active_assignee_required");
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.Created));
    }

    [Test]
    public async Task Start_AssignedTask_Returns204AndMovesToInProgress()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);

        await AssertStatusAsync(await ChangeStatusAsync(task.Id, "InProgress"), HttpStatusCode.NoContent);

        var started = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(started.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(started.UpdatedAt, Is.GreaterThan(task.UpdatedAt));
            Assert.That(started.CreatedAt, Is.EqualTo(task.CreatedAt));
        });
    }

    [Test]
    public async Task InProgressTask_CanBeReassignedToAnotherActiveUser()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync("Jane");
        var john = await CreateUserAsync("John");
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.InProgress);

        var response = await UpdateTaskAsync(task.Id, task.Title, task.Description, john.Id);

        await AssertStatusAsync(response, HttpStatusCode.NoContent);
        var updated = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Assignee?.Id, Is.EqualTo(john.Id));
            Assert.That(updated.Status, Is.EqualTo(TaskItemStatus.InProgress));
        });
    }

    [Test]
    public async Task InProgressTask_CannotBeUnassigned()
    {
        var project = await CreateProjectAsync();
        var jane = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, jane.Id, TaskItemStatus.InProgress);

        var response = await UpdateTaskAsync(task.Id, "New title", null, assigneeId: null);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.assignee_required");
        var unchanged = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Assignee?.Id, Is.EqualTo(jane.Id));
            Assert.That(unchanged.Title, Is.EqualTo(task.Title));
        });
    }

    [Test]
    public async Task FullWorkflow_StartCompleteClose_Succeeds()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(project.Id, user.Id);

        await AssertStatusAsync(await ChangeStatusAsync(task.Id, "InProgress"), HttpStatusCode.NoContent);
        await AssertStatusAsync(await ChangeStatusAsync(task.Id, "Completed"), HttpStatusCode.NoContent);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.Completed));

        await AssertStatusAsync(await ChangeStatusAsync(task.Id, "Closed"), HttpStatusCode.NoContent);
        Assert.That((await GetTaskAsync(task.Id)).Status, Is.EqualTo(TaskItemStatus.Closed));
    }

    [TestCase(TaskItemStatus.Created, "Completed")]
    [TestCase(TaskItemStatus.Created, "Closed")]
    [TestCase(TaskItemStatus.Created, "Created")]
    [TestCase(TaskItemStatus.InProgress, "Created")]
    [TestCase(TaskItemStatus.InProgress, "Closed")]
    [TestCase(TaskItemStatus.Completed, "Created")]
    [TestCase(TaskItemStatus.Completed, "InProgress")]
    [TestCase(TaskItemStatus.Closed, "Created")]
    [TestCase(TaskItemStatus.Closed, "InProgress")]
    [TestCase(TaskItemStatus.Closed, "Completed")]
    [TestCase(TaskItemStatus.Closed, "Closed")]
    public async Task ChangeStatus_NotTheNextTransition_Returns409AndKeepsStatus(
        TaskItemStatus current,
        string target)
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync();
        var task = await CreateTaskInStatusAsync(project.Id, user.Id, current);

        var response = await ChangeStatusAsync(task.Id, target);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "task.invalid_transition");
        var unchanged = await GetTaskAsync(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Status, Is.EqualTo(current));
            Assert.That(unchanged.UpdatedAt, Is.EqualTo(task.UpdatedAt));
        });
    }

    [Test]
    public async Task ChangeStatus_UnknownStatusValue_Returns400()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        await AssertProblemAsync(await ChangeStatusAsync(task.Id, "Reopened"), HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ChangeStatus_MissingStatus_Returns400()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.PatchAsync(
            $"/api/tasks/{task.Id}/status",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(problem.GetProperty("errors").TryGetProperty("status", out _), Is.True);
    }

    [Test]
    public async Task ChangeStatus_UnknownTask_Returns404()
    {
        await AssertProblemAsync(
            await ChangeStatusAsync(Guid.NewGuid(), "InProgress"),
            HttpStatusCode.NotFound,
            "task.not_found");
    }
}
