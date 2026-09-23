namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Commit boundary for an application use case. All changes staged by repositories during the
/// use case are persisted atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="PersistenceConflictException">
    /// The changes violate a uniqueness or referential constraint because the stored data changed
    /// concurrently.
    /// </exception>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
