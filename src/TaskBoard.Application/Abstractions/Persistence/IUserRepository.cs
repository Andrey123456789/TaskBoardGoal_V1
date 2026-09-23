using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for users. "Ordinary" users are all users except the system Deleted User.
/// </summary>
public interface IUserRepository
{
    Task<IReadOnlyList<User>> ListOrdinaryAsync(
        CancellationToken cancellationToken);

    /// <summary>Returns a tracked ordinary user, or <c>null</c> for an unknown ID or the Deleted User.</summary>
    Task<User?> GetOrdinaryByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> OrdinaryUserExistsAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>Case-insensitive email lookup across all users, optionally ignoring one user.</summary>
    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
