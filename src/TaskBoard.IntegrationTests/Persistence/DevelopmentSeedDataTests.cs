using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure.Persistence.Seeding;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Persistence;

/// <summary>
/// Verifies that the maintained Development seed dataset satisfies the specification minimums and
/// never overwrites existing data. Other tests do not depend on this data.
/// </summary>
[TestFixture]
public sealed class DevelopmentSeedDataTests : ApiTestBase
{
    [Test]
    public async Task Seed_EmptyDatabase_CreatesRepresentativeDataset()
    {
        var seeded = await Factory.QueryDatabaseAsync(db => DbSeeder.SeedAsync(db));
        Assert.That(seeded, Is.True);

        var users = await ListUsersAsync();
        var tasks = await ListTasksAsync();
        var relations = await Factory.QueryDatabaseAsync(db => db.TaskRelations.AsNoTracking().ToListAsync());

        int CountWith(TaskItemStatus status) => tasks.Count(x => x.Status == status);

        Assert.Multiple(() =>
        {
            Assert.That(users, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(users.Select(x => x.Id), Does.Not.Contain(User.DeletedUserId));
            Assert.That(tasks.Select(x => x.Project.Id).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(tasks.Count(x => x.Assignee is null), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountWith(TaskItemStatus.Created), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountWith(TaskItemStatus.InProgress), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountWith(TaskItemStatus.Completed), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountWith(TaskItemStatus.Closed), Is.GreaterThanOrEqualTo(1));
            Assert.That(
                tasks.Where(x => x.Assignee is not null).Select(x => x.Assignee!.Id).Distinct().Count(),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(relations, Is.Not.Empty);
        });

        var completedIds = tasks.Where(x => x.Status == TaskItemStatus.Completed).Select(x => x.Id).ToHashSet();
        var openIds = tasks
            .Where(x => x.Status is TaskItemStatus.Created or TaskItemStatus.InProgress)
            .Select(x => x.Id)
            .ToHashSet();

        var hasCompletedTaskWithFollowUp = relations.Any(r =>
            (completedIds.Contains(r.FirstTaskId) && openIds.Contains(r.SecondTaskId)) ||
            (completedIds.Contains(r.SecondTaskId) && openIds.Contains(r.FirstTaskId)));

        Assert.That(hasCompletedTaskWithFollowUp, Is.True);
    }

    [Test]
    public async Task Seed_DatabaseWithExistingData_LeavesItUnchanged()
    {
        await CreateProjectAsync("Existing");

        var seeded = await Factory.QueryDatabaseAsync(db => DbSeeder.SeedAsync(db));

        var tasks = await ListTasksAsync();
        var users = await ListUsersAsync();
        var projectNames = await Factory.QueryDatabaseAsync(db => db.Projects.Select(x => x.Name).ToListAsync());

        Assert.Multiple(() =>
        {
            Assert.That(seeded, Is.False);
            Assert.That(tasks, Is.Empty);
            Assert.That(users, Is.Empty);
            Assert.That(projectNames, Is.EqualTo(new[] { "Existing" }));
        });
    }
}
