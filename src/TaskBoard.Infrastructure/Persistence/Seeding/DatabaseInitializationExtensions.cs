using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Infrastructure.Persistence.Seeding;

public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Populates an empty, fully migrated Development database with demo data. Existing data is
    /// never modified, and the schema is never created or migrated here.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbSeeder));

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendingMigrations.Count > 0)
        {
            logger.LogWarning(
                "Development seeding skipped: the database is missing {PendingMigrationCount} migration(s). " +
                "Run 'dotnet ef database update --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api' and restart",
                pendingMigrations.Count);

            return;
        }

        if (await DbSeeder.SeedAsync(db, cancellationToken))
        {
            logger.LogInformation("Development seed data created");
        }
        else
        {
            logger.LogInformation("Development seeding skipped: the database already contains data");
        }
    }
}
