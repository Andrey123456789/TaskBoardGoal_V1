using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<IReadOnlyList<User>> ListOrdinaryAsync(
        CancellationToken cancellationToken)
    {
        return await db.Users
            .AsNoTracking()
            .Where(x => x.Id != User.DeletedUserId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetOrdinaryByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == User.DeletedUserId)
        {
            return Task.FromResult<User?>(null);
        }

        return db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> OrdinaryUserExistsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Users.AnyAsync(
            x => x.Id == id && x.Id != User.DeletedUserId,
            cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        // The Email column uses a case-insensitive collation, so equality ignores letter case.
        var query = db.Users.Where(x => x.Email == email);

        if (excludedUserId is { } excludedId)
        {
            query = query.Where(x => x.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
