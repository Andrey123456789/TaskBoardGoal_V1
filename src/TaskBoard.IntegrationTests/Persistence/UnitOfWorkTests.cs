using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Persistence;

/// <summary>
/// Verifies against SQL Server that constraint violations caused by concurrent writes surface as
/// <see cref="PersistenceConflictException"/> (mapped to 409 by the API) instead of a generic failure.
/// </summary>
[TestFixture]
public sealed class UnitOfWorkTests : ApiTestBase
{
    [Test]
    public async Task SaveChanges_WhenUniqueEmailWasTakenConcurrently_ThrowsPersistenceConflict()
    {
        await SaveUserInNewScopeAsync(User.Create("First", "same@example.com", DateTimeOffset.UtcNow));

        Assert.ThrowsAsync<PersistenceConflictException>(() =>
            SaveUserInNewScopeAsync(User.Create("Second", "SAME@example.com", DateTimeOffset.UtcNow)));

        Assert.That(await ListUsersAsync(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SaveChanges_WhenProjectGainedTasksConcurrently_ThrowsPersistenceConflict()
    {
        var project = await CreateProjectAsync();
        await CreateTaskAsync(project.Id);

        await using var scope = Factory.Services.CreateAsyncScope();
        var projects = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Simulates a delete that passed the "has no tasks" check before a task was added.
        var tracked = await projects.GetByIdAsync(project.Id, CancellationToken.None);
        projects.Remove(tracked!);

        Assert.ThrowsAsync<PersistenceConflictException>(() => unitOfWork.SaveChangesAsync());
    }

    private static async Task SaveUserInNewScopeAsync(User user)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        users.Add(user);
        await unitOfWork.SaveChangesAsync();
    }
}
