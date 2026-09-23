using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(
        CancellationToken cancellationToken);

    /// <summary>Returns a tracked project, or <c>null</c> when it does not exist.</summary>
    Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>Case-insensitive name lookup, optionally ignoring one project.</summary>
    Task<bool> NameExistsAsync(
        string name,
        Guid? excludedProjectId,
        CancellationToken cancellationToken);

    void Add(Project project);

    void Remove(Project project);
}
