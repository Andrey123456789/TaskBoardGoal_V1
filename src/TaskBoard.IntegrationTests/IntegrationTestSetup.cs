using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests;

/// <summary>
/// Creates one isolated test database (from the EF Core migrations) for the whole test run.
/// Individual tests reset the data they depend on.
/// </summary>
[SetUpFixture]
public sealed class IntegrationTestSetup
{
    public static TaskBoardApiFactory Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        Factory = new TaskBoardApiFactory();
        await Factory.CreateDatabaseAsync();
    }

    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        try
        {
            await Factory.DropDatabaseAsync();
        }
        finally
        {
            await Factory.DisposeAsync();
        }
    }
}
