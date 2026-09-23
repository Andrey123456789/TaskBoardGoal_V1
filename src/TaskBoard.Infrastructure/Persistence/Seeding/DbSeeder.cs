using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development demo data. The system Deleted User is not created here; it is reference data that
/// the migrations insert in every environment.
/// </summary>
internal static class DbSeeder
{
    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Seeds an empty database. Returns <c>false</c> when data already exists.</summary>
    public static async Task<bool> SeedAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (await db.Projects.AnyAsync(cancellationToken) ||
            await db.Tasks.AnyAsync(cancellationToken) ||
            await db.Users.AnyAsync(x => x.Id != User.DeletedUserId, cancellationToken))
        {
            return false;
        }

        var alice = User.Create("Alice Johnson", "alice.johnson@example.com", BaseTime);
        var bob = User.Create("Bob Smith", "bob.smith@example.com", BaseTime.AddMinutes(5));
        var carol = User.Create("Carol Diaz", "carol.diaz@example.com", BaseTime.AddMinutes(10));

        var website = Project.Create(
            "Website Redesign",
            "Refresh the public marketing website.",
            BaseTime.AddMinutes(30));

        var mobileApp = Project.Create(
            "Mobile App",
            "First public release of the customer mobile app.",
            BaseTime.AddMinutes(40));

        var landingPage = CreateTask(
            "Design new landing page",
            "Wireframes and final visual design for the new landing page.",
            website, alice, TaskItemStatus.Completed, day: 1);

        var landingPageFollowUp = CreateTask(
            "Fix landing page accessibility issues",
            "Follow-up to the landing page design: contrast and keyboard navigation findings.",
            website, alice, TaskItemStatus.Created, day: 6);

        var analytics = CreateTask(
            "Set up web analytics",
            "Track page views and sign-up conversions.",
            website, bob, TaskItemStatus.InProgress, day: 2);

        var aboutPage = CreateTask(
            "Write content for the About page",
            null,
            website, assignee: null, TaskItemStatus.Created, day: 3);

        var blogMigration = CreateTask(
            "Migrate blog posts",
            "Move existing posts from the old CMS.",
            website, carol, TaskItemStatus.Closed, day: 1);

        var offlineSync = CreateTask(
            "Implement offline sync",
            "Queue changes locally and synchronize when the device is back online.",
            mobileApp, carol, TaskItemStatus.InProgress, day: 2);

        var profileScreen = CreateTask(
            "Build profile settings screen",
            null,
            mobileApp, bob, TaskItemStatus.Completed, day: 1);

        var releaseChecklist = CreateTask(
            "Prepare release checklist",
            "Store listing, screenshots and release notes.",
            mobileApp, bob, TaskItemStatus.Created, day: 5);

        var crashReporting = CreateTask(
            "Integrate crash reporting",
            null,
            mobileApp, assignee: null, TaskItemStatus.Created, day: 4);

        db.Users.AddRange(alice, bob, carol);
        db.Projects.AddRange(website, mobileApp);
        db.Tasks.AddRange(
            landingPage,
            landingPageFollowUp,
            analytics,
            aboutPage,
            blogMigration,
            offlineSync,
            profileScreen,
            releaseChecklist,
            crashReporting);

        db.TaskRelations.AddRange(
            TaskRelation.Create(landingPage.Id, landingPageFollowUp.Id),
            TaskRelation.Create(analytics.Id, offlineSync.Id),
            TaskRelation.Create(profileScreen.Id, releaseChecklist.Id));

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Creates a task and moves it through the workflow to <paramref name="status"/>, one day per
    /// transition, so that every seeded task respects the domain rules.
    /// </summary>
    private static TaskItem CreateTask(
        string title,
        string? description,
        Project project,
        User? assignee,
        TaskItemStatus status,
        int day)
    {
        var createdAt = BaseTime.AddDays(day);
        var task = TaskItem.Create(title, description, project.Id, assignee?.Id, createdAt);

        var transitionTime = createdAt;
        while (task.Status != status)
        {
            var next = task.NextStatus
                ?? throw new InvalidOperationException($"Seed task '{title}' cannot reach {status}.");

            transitionTime = transitionTime.AddDays(1);

            if (task.TransitionTo(next, transitionTime) != TaskTransitionOutcome.Transitioned)
            {
                throw new InvalidOperationException($"Seed task '{title}' cannot move to {next}.");
            }
        }

        return task;
    }
}
