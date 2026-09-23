using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for tasks and the symmetric relationships between them.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskSummaryDto>> ListSummariesAsync(
        TaskFilter filter,
        CancellationToken cancellationToken);

    Task<TaskDetailsDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>Returns a tracked task, or <c>null</c> when it does not exist.</summary>
    Task<TaskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>Returns the subset of <paramref name="ids"/> that identify existing tasks.</summary>
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>Returns tracked tasks currently assigned to the user.</summary>
    Task<IReadOnlyList<TaskItem>> ListAssignedToAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> AnyInProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    /// <summary>Returns tracked relationships in which the task participates on either side.</summary>
    Task<IReadOnlyList<TaskRelation>> ListRelationsAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    void Add(TaskItem task);

    void Remove(TaskItem task);

    void AddRelation(TaskRelation relation);

    void RemoveRelation(TaskRelation relation);
}
