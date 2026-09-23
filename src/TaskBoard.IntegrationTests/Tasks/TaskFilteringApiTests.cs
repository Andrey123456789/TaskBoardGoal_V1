using System.Net;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Enums;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Tasks;

[TestFixture]
public sealed class TaskFilteringApiTests : ApiTestBase
{
    private Guid _websiteId;
    private Guid _mobileId;
    private Guid _janeId;
    private Guid _johnId;
    private TaskDetailsDto _websiteJaneInProgress = null!;
    private TaskDetailsDto _websiteJohnCreated = null!;
    private TaskDetailsDto _websiteUnassigned = null!;
    private TaskDetailsDto _mobileJaneCreated = null!;
    private TaskDetailsDto _mobileJohnCompleted = null!;

    [SetUp]
    public async Task CreateBoardAsync()
    {
        _websiteId = (await CreateProjectAsync("Website")).Id;
        _mobileId = (await CreateProjectAsync("Mobile")).Id;
        _janeId = (await CreateUserAsync("Jane")).Id;
        _johnId = (await CreateUserAsync("John")).Id;

        _websiteJaneInProgress = await CreateTaskInStatusAsync(_websiteId, _janeId, TaskItemStatus.InProgress);
        _websiteJohnCreated = await CreateTaskInStatusAsync(_websiteId, _johnId, TaskItemStatus.Created);
        _websiteUnassigned = await CreateTaskAsync(_websiteId);
        _mobileJaneCreated = await CreateTaskInStatusAsync(_mobileId, _janeId, TaskItemStatus.Created);
        _mobileJohnCompleted = await CreateTaskInStatusAsync(_mobileId, _johnId, TaskItemStatus.Completed);
    }

    [Test]
    public async Task ListTasks_WithoutFilters_ReturnsAllTasksWithProjectAndAssigneeNames()
    {
        var tasks = await ListTasksAsync();

        Assert.That(tasks, Has.Count.EqualTo(5));

        var summary = tasks.Single(x => x.Id == _websiteJaneInProgress.Id);
        Assert.Multiple(() =>
        {
            Assert.That(summary.Title, Is.EqualTo(_websiteJaneInProgress.Title));
            Assert.That(summary.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(summary.Project, Is.EqualTo(new ProjectSummaryDto(_websiteId, "Website")));
            Assert.That(summary.Assignee, Is.EqualTo(new AssigneeDto(_janeId, "Jane", IsDeletedUser: false)));
            Assert.That(tasks.Single(x => x.Id == _websiteUnassigned.Id).Assignee, Is.Null);
        });
    }

    [Test]
    public async Task ListTasks_FilteredByProject_ReturnsOnlyThatProject()
    {
        var tasks = await ListTasksAsync($"?projectId={_mobileId}");

        Assert.That(tasks.Select(x => x.Id), Is.EquivalentTo(new[] { _mobileJaneCreated.Id, _mobileJohnCompleted.Id }));
    }

    [Test]
    public async Task ListTasks_FilteredByAssignee_ReturnsOnlyThatAssignee()
    {
        var tasks = await ListTasksAsync($"?assigneeId={_janeId}");

        Assert.That(tasks.Select(x => x.Id), Is.EquivalentTo(new[] { _websiteJaneInProgress.Id, _mobileJaneCreated.Id }));
    }

    [Test]
    public async Task ListTasks_FilteredByStatus_ReturnsOnlyThatStatus()
    {
        var tasks = await ListTasksAsync("?status=Created");

        Assert.That(
            tasks.Select(x => x.Id),
            Is.EquivalentTo(new[] { _websiteJohnCreated.Id, _websiteUnassigned.Id, _mobileJaneCreated.Id }));
    }

    [Test]
    public async Task ListTasks_WithCombinedFilters_AppliesAllOfThem()
    {
        var tasks = await ListTasksAsync($"?projectId={_websiteId}&assigneeId={_johnId}&status=Created");

        Assert.That(tasks.Select(x => x.Id), Is.EqualTo(new[] { _websiteJohnCreated.Id }));
    }

    [Test]
    public async Task ListTasks_WithNoMatches_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync($"/api/tasks?projectId={_mobileId}&status=Closed");

        await AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[]"));
    }

    [Test]
    public async Task ListTasks_WithUnknownProject_Returns200WithEmptyArray()
    {
        Assert.That(await ListTasksAsync($"?projectId={Guid.NewGuid()}"), Is.Empty);
    }

    [TestCase("?status=Reopened")]
    [TestCase("?status=42")]
    [TestCase("?projectId=not-a-guid")]
    public async Task ListTasks_WithInvalidFilterValue_Returns400(string query)
    {
        await AssertProblemAsync(await Client.GetAsync($"/api/tasks{query}"), HttpStatusCode.BadRequest);
    }
}
