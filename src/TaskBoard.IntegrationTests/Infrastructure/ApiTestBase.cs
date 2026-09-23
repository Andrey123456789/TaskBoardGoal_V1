using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Enums;

namespace TaskBoard.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for black-box API tests. Every test starts from an empty database that contains
/// only the system Deleted User, and creates the data it needs through the public API.
/// </summary>
public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected static TaskBoardApiFactory Factory => IntegrationTestSetup.Factory;

    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public async Task ResetStateAsync()
    {
        await Factory.ResetDataAsync();
        Factory.Logs.Clear();
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void DisposeClient() => Client.Dispose();

    protected async Task<UserDto> CreateUserAsync(string? name = null, string? email = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = name ?? $"User {suffix}", email = email ?? $"user.{suffix}@example.com" },
            Json);

        return await ReadAsync<UserDto>(response, HttpStatusCode.Created);
    }

    protected async Task<ProjectDto> CreateProjectAsync(string? name = null, string? description = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/projects",
            new { name = name ?? $"Project {Guid.NewGuid():N}", description },
            Json);

        return await ReadAsync<ProjectDto>(response, HttpStatusCode.Created);
    }

    protected async Task<TaskDetailsDto> CreateTaskAsync(
        Guid projectId,
        Guid? assigneeId = null,
        string? title = null,
        IEnumerable<Guid>? relatedTaskIds = null,
        string? description = null)
    {
        var response = await PostTaskAsync(new
        {
            title = title ?? $"Task {Guid.NewGuid():N}",
            description,
            projectId,
            assigneeId,
            relatedTaskIds = relatedTaskIds?.ToArray(),
        });

        return await ReadAsync<TaskDetailsDto>(response, HttpStatusCode.Created);
    }

    /// <summary>Creates a task and moves it through the workflow to <paramref name="status"/>.</summary>
    protected async Task<TaskDetailsDto> CreateTaskInStatusAsync(
        Guid projectId,
        Guid? assigneeId,
        TaskItemStatus status,
        string? title = null)
    {
        var task = await CreateTaskAsync(projectId, assigneeId, title);

        foreach (var next in new[] { TaskItemStatus.InProgress, TaskItemStatus.Completed, TaskItemStatus.Closed })
        {
            if (next > status)
            {
                break;
            }

            await AssertStatusAsync(await ChangeStatusAsync(task.Id, next.ToString()), HttpStatusCode.NoContent);
        }

        return await GetTaskAsync(task.Id);
    }

    protected Task<HttpResponseMessage> PostTaskAsync(object body) =>
        Client.PostAsJsonAsync("/api/tasks", body, Json);

    protected Task<HttpResponseMessage> UpdateTaskAsync(
        Guid taskId,
        string title,
        string? description,
        Guid? assigneeId) =>
        Client.PutAsJsonAsync($"/api/tasks/{taskId}", new { title, description, assigneeId }, Json);

    protected Task<HttpResponseMessage> ChangeStatusAsync(Guid taskId, string status) =>
        Client.PatchAsJsonAsync($"/api/tasks/{taskId}/status", new { status }, Json);

    protected Task<HttpResponseMessage> ReplaceRelatedTasksAsync(Guid taskId, params Guid[] relatedTaskIds) =>
        Client.PutAsJsonAsync($"/api/tasks/{taskId}/related-tasks", new { relatedTaskIds }, Json);

    protected async Task<TaskDetailsDto> GetTaskAsync(Guid taskId) =>
        await ReadAsync<TaskDetailsDto>(await Client.GetAsync($"/api/tasks/{taskId}"), HttpStatusCode.OK);

    protected async Task<List<TaskSummaryDto>> ListTasksAsync(string query = "") =>
        await ReadAsync<List<TaskSummaryDto>>(await Client.GetAsync($"/api/tasks{query}"), HttpStatusCode.OK);

    protected async Task<List<UserDto>> ListUsersAsync() =>
        await ReadAsync<List<UserDto>>(await Client.GetAsync("/api/users"), HttpStatusCode.OK);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        await AssertStatusAsync(response, expectedStatus);

        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.That(value, Is.Not.Null);

        return value!;
    }

    protected static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        if (response.StatusCode != expectedStatus)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expectedStatus} {expectedStatus} but got {(int)response.StatusCode} {response.StatusCode}: {body}");
        }
    }

    /// <summary>Asserts a ProblemDetails response and, optionally, its machine-readable code.</summary>
    protected static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedCode = null)
    {
        await AssertStatusAsync(response, expectedStatus);

        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo((int)expectedStatus));

        if (expectedCode is not null)
        {
            Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo(expectedCode));
        }

        return problem;
    }
}
