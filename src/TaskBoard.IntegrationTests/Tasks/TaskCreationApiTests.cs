using System.Net;
using System.Net.Http.Json;
using System.Text;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Tasks;

[TestFixture]
public sealed class TaskCreationApiTests : ApiTestBase
{
    [Test]
    public async Task CreateTask_Unassigned_Returns201InCreatedStatus()
    {
        var project = await CreateProjectAsync("Website");

        var response = await PostTaskAsync(new
        {
            title = " Write copy ",
            description = "About page",
            projectId = project.Id,
            assigneeId = (Guid?)null,
        });

        var created = await ReadAsync<TaskDetailsDto>(response, HttpStatusCode.Created);

        Assert.Multiple(() =>
        {
            Assert.That(created.Title, Is.EqualTo("Write copy"));
            Assert.That(created.Description, Is.EqualTo("About page"));
            Assert.That(created.Status, Is.EqualTo(TaskItemStatus.Created));
            Assert.That(created.Project, Is.EqualTo(new ProjectSummaryDto(project.Id, "Website")));
            Assert.That(created.Assignee, Is.Null);
            Assert.That(created.RelatedTasks, Is.Empty);
            Assert.That(created.UpdatedAt, Is.EqualTo(created.CreatedAt));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/tasks/{created.Id}"));
        });
    }

    [Test]
    public async Task CreateTask_AssignedToUser_ReturnsAssigneeSummary()
    {
        var project = await CreateProjectAsync();
        var user = await CreateUserAsync("Jane");

        var created = await CreateTaskAsync(project.Id, user.Id);

        Assert.That(created.Assignee, Is.EqualTo(new AssigneeDto(user.Id, "Jane", IsDeletedUser: false)));
    }

    [Test]
    public async Task CreateTask_WithRelatedTaskIds_CreatesSymmetricRelationshipsOnce()
    {
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        var first = await CreateTaskAsync(project.Id);
        var second = await CreateTaskAsync(otherProject.Id);

        var created = await CreateTaskAsync(project.Id, relatedTaskIds: [first.Id, second.Id, first.Id]);

        Assert.That(created.RelatedTasks.Select(x => x.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
        Assert.That((await GetTaskAsync(first.Id)).RelatedTasks.Select(x => x.Id), Is.EqualTo(new[] { created.Id }));
        Assert.That((await GetTaskAsync(second.Id)).RelatedTasks.Select(x => x.Id), Is.EqualTo(new[] { created.Id }));
    }

    [Test]
    public async Task CreateTask_WithEmptyRelatedTaskIds_Returns201()
    {
        var project = await CreateProjectAsync();

        var created = await CreateTaskAsync(project.Id, relatedTaskIds: []);

        Assert.That(created.RelatedTasks, Is.Empty);
    }

    [Test]
    public async Task CreateTask_WithNonExistentProject_Returns422()
    {
        var response = await PostTaskAsync(new { title = "Task", projectId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.project_not_found");
    }

    [Test]
    public async Task CreateTask_WithNonExistentAssignee_Returns422()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "Task", projectId = project.Id, assigneeId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.assignee_not_found");
    }

    [Test]
    public async Task CreateTask_AssignedToDeletedUser_Returns422()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "Task", projectId = project.Id, assigneeId = User.DeletedUserId });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.deleted_user_not_assignable");
    }

    [Test]
    public async Task CreateTask_WithNonExistentRelatedTask_Returns422AndCreatesNothing()
    {
        var project = await CreateProjectAsync();
        var existing = await CreateTaskAsync(project.Id);

        var response = await PostTaskAsync(new
        {
            title = "Task",
            projectId = project.Id,
            relatedTaskIds = new[] { existing.Id, Guid.NewGuid() },
        });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "task.related_tasks_not_found");
        Assert.That(await ListTasksAsync(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateTask_WithInitialStatus_Returns400()
    {
        var project = await CreateProjectAsync();

        var response = await PostTaskAsync(new { title = "Task", projectId = project.Id, status = "InProgress" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(await ListTasksAsync(), Is.Empty);
    }

    [Test]
    public async Task CreateTask_WithoutTitleAndProject_Returns400WithFieldErrors()
    {
        var response = await PostTaskAsync(new { title = "  " });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var errors = problem.GetProperty("errors");

        Assert.Multiple(() =>
        {
            Assert.That(errors.TryGetProperty("title", out _), Is.True);
            Assert.That(errors.TryGetProperty("projectId", out _), Is.True);
        });
    }

    [Test]
    public async Task CreateTask_WithMalformedJson_Returns400()
    {
        using var content = new StringContent("{ \"title\": ", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/tasks", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTask_WithInvalidGuid_Returns400()
    {
        var response = await PostTaskAsync(new { title = "Task", projectId = "not-a-guid" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetTask_UnknownId_Returns404()
    {
        await AssertProblemAsync(
            await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}"),
            HttpStatusCode.NotFound,
            "task.not_found");
    }
}
