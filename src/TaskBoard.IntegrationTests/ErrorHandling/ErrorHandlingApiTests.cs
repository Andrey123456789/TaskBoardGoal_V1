using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Results;
using TaskBoard.Application.Services;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.ErrorHandling;

[TestFixture]
public sealed class ErrorHandlingApiTests : ApiTestBase
{
    private const string SensitiveMessage =
        "Boom: SELECT * FROM Users; Server=prod-sql;Password=hunter2; C:\\internal\\secrets\\config.json";

    [Test]
    public async Task BusinessConflict_ReturnsProblemDetailsWithCodeAndTraceId()
    {
        await CreateUserAsync(email: "jane@example.com");

        var response = await Client.PostAsJsonAsync("/api/users", new { name = "Jane", email = "jane@example.com" }, Json);

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict, "user.duplicate_email");
        Assert.Multiple(() =>
        {
            Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
            Assert.That(problem.GetProperty("detail").GetString(), Is.Not.Empty);
            Assert.That(problem.TryGetProperty("traceId", out _), Is.True);
        });
    }

    [Test]
    public async Task UnexpectedException_Returns500SanitizedProblemDetailsAndLogsTheException()
    {
        using var factory = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IProjectService, ThrowingProjectService>()));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        await AssertStatusAsync(response, HttpStatusCode.InternalServerError);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("\"status\":500"));
            Assert.That(body, Does.Contain("An unexpected error occurred"));
            Assert.That(body, Does.Not.Contain("Boom"));
            Assert.That(body, Does.Not.Contain("SELECT"));
            Assert.That(body, Does.Not.Contain("hunter2"));
            Assert.That(body, Does.Not.Contain("Server="));
            Assert.That(body, Does.Not.Contain("internal"));
            Assert.That(body, Does.Not.Contain("InvalidOperationException"));
            Assert.That(body, Does.Not.Contain("inner"));
            Assert.That(body, Does.Not.Contain(" at "));
        });

        var errorEvents = Factory.Logs.Events
            .Where(e => e.Level == LogEventLevel.Error && e.Exception is InvalidOperationException)
            .ToList();

        Assert.That(errorEvents, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(errorEvents[0].Exception!.Message, Is.EqualTo(SensitiveMessage));
            Assert.That(errorEvents[0].Properties["RequestPath"].ToString(), Does.Contain("/api/projects"));
        });
    }

    [Test]
    public async Task ConcurrentPersistenceConflict_Returns409ProblemDetails()
    {
        using var factory = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IProjectService, ConflictingProjectService>()));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", new { name = "Website" }, Json);

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict, "persistence.conflict");
        Assert.That(problem.GetRawText(), Does.Not.Contain("IX_Projects_Name"));
    }

    [Test]
    public async Task MalformedJson_Returns400WithoutInternalTypeNames()
    {
        using var content = new StringContent("{ \"name\": 42 }", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/projects", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("TaskBoard."));
    }

    [Test]
    public async Task UnknownRoute_Returns404ProblemDetails()
    {
        await AssertProblemAsync(await Client.GetAsync("/api/unknown"), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SuccessfulRequest_IsLoggedWithCompletionInformation()
    {
        await AssertStatusAsync(await Client.GetAsync("/api/projects"), HttpStatusCode.OK);

        var requestEvent = await Factory.Logs.WaitForAsync(
            e => e.Properties.TryGetValue("RequestPath", out var path) &&
                path.ToString() == "\"/api/projects\"" &&
                e.Properties.ContainsKey("StatusCode"),
            TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(requestEvent.Properties["RequestMethod"].ToString(), Is.EqualTo("\"GET\""));
            Assert.That(requestEvent.Properties["StatusCode"].ToString(), Is.EqualTo("200"));
            Assert.That(requestEvent.Properties.ContainsKey("Elapsed"), Is.True);
        });
    }

    private sealed class ThrowingProjectService : IProjectService
    {
        public Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                SensitiveMessage,
                new InvalidOperationException("inner failure details"));

        public Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<ProjectDto>> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> UpdateAsync(Guid id, SaveProjectRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ConflictingProjectService : IProjectService
    {
        public Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<ProjectDto>> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken) =>
            throw new PersistenceConflictException(
                "The changes conflict with data that was saved concurrently.",
                new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.Projects' with unique index 'IX_Projects_Name'."));

        public Task<Result> UpdateAsync(Guid id, SaveProjectRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
