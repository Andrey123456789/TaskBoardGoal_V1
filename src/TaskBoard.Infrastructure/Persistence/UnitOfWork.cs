using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;

namespace TaskBoard.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // SQL Server error numbers for duplicate keys (unique index / constraint) and for
    // FOREIGN KEY or CHECK constraint violations.
    private const int DuplicateKeyRowError = 2601;
    private const int UniqueConstraintViolationError = 2627;
    private const int ConstraintViolationError = 547;

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(
                "The data was changed or deleted concurrently.",
                exception);
        }
        catch (DbUpdateException exception) when (IsConstraintViolation(exception))
        {
            throw new PersistenceConflictException(
                "The changes conflict with data that was saved concurrently.",
                exception);
        }
    }

    private static bool IsConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException
        {
            Number: DuplicateKeyRowError or UniqueConstraintViolationError or ConstraintViolationError,
        };
}
