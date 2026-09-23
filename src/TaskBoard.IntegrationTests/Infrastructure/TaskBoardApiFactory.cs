using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against an isolated SQL Server database that is created from the
/// migrations for this test run and dropped afterwards. Never touches the Development database.
/// </summary>
public sealed class TaskBoardApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedCorsOrigin = "http://localhost:4200";

    /// <summary>
    /// Optional base connection string for a SQL Server instance other than LocalDB. The database
    /// name is always replaced with a unique, test-owned name.
    /// </summary>
    private const string ConnectionStringVariable = "TASKBOARD_TEST_SQLSERVER";

    private const string DefaultServer =
        @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;";

    public TaskBoardApiFactory()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DefaultServer;

        ConnectionString = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"TaskBoardGoal_Tests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public CapturingLogSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedCorsOrigin);
        builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(Logs));
    }

    public async Task CreateDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
    }

    public async Task DropDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
    }

    /// <summary>Removes all data except the system Deleted User reference row.</summary>
    public async Task ResetDataAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.TaskRelations.ExecuteDeleteAsync();
        await db.Tasks.ExecuteDeleteAsync();
        await db.Projects.ExecuteDeleteAsync();
        await db.Users.Where(x => x.Id != User.DeletedUserId).ExecuteDeleteAsync();
    }

    internal async Task<T> QueryDatabaseAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await query(db);
    }
}
