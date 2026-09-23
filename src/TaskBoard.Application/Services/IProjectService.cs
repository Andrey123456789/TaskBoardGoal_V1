using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Results;

namespace TaskBoard.Application.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(
        CancellationToken cancellationToken);

    Task<Result<ProjectDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Result<ProjectDto>> CreateAsync(
        SaveProjectRequest request,
        CancellationToken cancellationToken);

    Task<Result> UpdateAsync(
        Guid id,
        SaveProjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>Deletes the project only when it contains no tasks; tasks are never cascade-deleted.</summary>
    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
