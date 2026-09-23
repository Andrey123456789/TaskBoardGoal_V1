using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Projects;

[TestFixture]
public sealed class ProjectsApiTests : ApiTestBase
{
    [Test]
    public async Task CreateProject_WithValidRequest_Returns201WithLocationAndProject()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/projects",
            new { name = " Website ", description = "Marketing site" },
            Json);

        var created = await ReadAsync<ProjectDto>(response, HttpStatusCode.Created);

        Assert.Multiple(() =>
        {
            Assert.That(created.Name, Is.EqualTo("Website"));
            Assert.That(created.Description, Is.EqualTo("Marketing site"));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/projects/{created.Id}"));
        });
    }

    [Test]
    public async Task CreateProject_WithoutDescription_StoresNullDescription()
    {
        var created = await CreateProjectAsync("Website", description: "   ");

        Assert.That(created.Description, Is.Null);
    }

    [Test]
    public async Task CreateProject_WithDuplicateName_Returns409()
    {
        await CreateProjectAsync("Website");

        var response = await Client.PostAsJsonAsync("/api/projects", new { name = "website" }, Json);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "project.duplicate_name");
    }

    [Test]
    public async Task CreateProject_WithoutName_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name = "", description = "x" }, Json);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(problem.GetProperty("errors").TryGetProperty("name", out _), Is.True);
    }

    [Test]
    public async Task UpdateProject_WithValidRequest_Returns204AndPersistsChanges()
    {
        var project = await CreateProjectAsync("Website", "Old");

        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{project.Id}",
            new { name = "Website v2", description = "New" },
            Json);

        await AssertStatusAsync(response, HttpStatusCode.NoContent);

        var fetched = await ReadAsync<ProjectDto>(
            await Client.GetAsync($"/api/projects/{project.Id}"),
            HttpStatusCode.OK);

        Assert.Multiple(() =>
        {
            Assert.That(fetched.Name, Is.EqualTo("Website v2"));
            Assert.That(fetched.Description, Is.EqualTo("New"));
        });
    }

    [Test]
    public async Task UpdateProject_ToNameOfAnotherProject_Returns409()
    {
        await CreateProjectAsync("Website");
        var mobile = await CreateProjectAsync("Mobile");

        var response = await Client.PutAsJsonAsync($"/api/projects/{mobile.Id}", new { name = "WEBSITE" }, Json);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "project.duplicate_name");
    }

    [Test]
    public async Task UpdateProject_UnknownId_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"/api/projects/{Guid.NewGuid()}", new { name = "X" }, Json);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "project.not_found");
    }

    [Test]
    public async Task DeleteProject_WithoutTasks_Returns204AndRemovesProject()
    {
        var project = await CreateProjectAsync();

        await AssertStatusAsync(await Client.DeleteAsync($"/api/projects/{project.Id}"), HttpStatusCode.NoContent);

        await AssertProblemAsync(await Client.GetAsync($"/api/projects/{project.Id}"), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProject_WithTasks_Returns409AndKeepsProjectAndTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id);

        var response = await Client.DeleteAsync($"/api/projects/{project.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "project.has_tasks");
        await AssertStatusAsync(await Client.GetAsync($"/api/projects/{project.Id}"), HttpStatusCode.OK);
        Assert.That((await GetTaskAsync(task.Id)).Project.Id, Is.EqualTo(project.Id));
    }

    [Test]
    public async Task ListProjects_ReturnsProjectsOrderedByName()
    {
        var mobile = await CreateProjectAsync("Mobile");
        var api = await CreateProjectAsync("API");

        var projects = await ReadAsync<List<ProjectDto>>(await Client.GetAsync("/api/projects"), HttpStatusCode.OK);

        Assert.That(projects.Select(x => x.Id), Is.EqualTo(new[] { api.Id, mobile.Id }));
    }
}
