using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> ListAsync(
        CancellationToken cancellationToken)
    {
        return await db.Projects
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Projects.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludedProjectId,
        CancellationToken cancellationToken)
    {
        // The Name column uses a case-insensitive collation, so equality ignores letter case.
        var query = db.Projects.Where(x => x.Name == name);

        if (excludedProjectId is { } excludedId)
        {
            query = query.Where(x => x.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(Project project) => db.Projects.Add(project);

    public void Remove(Project project) => db.Projects.Remove(project);
}
