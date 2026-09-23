using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Results;

namespace TaskBoard.Application.Services;

public interface ITaskService
{
    Task<Result<IReadOnlyList<TaskSummaryDto>>> ListAsync(
        TaskFilter filter,
        CancellationToken cancellationToken);

    Task<Result<TaskDetailsDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Result<TaskDetailsDto>> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken);

    /// <summary>Edits title, description and assignee of a Created or InProgress task.</summary>
    Task<Result> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken);

    /// <summary>Moves the task to the immediately following workflow status.</summary>
    Task<Result> ChangeStatusAsync(
        Guid id,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the complete set of related tasks. Allowed in every status; repeated identical
    /// requests produce the same final state.
    /// </summary>
    Task<Result> ReplaceRelatedTasksAsync(
        Guid id,
        ReplaceRelatedTasksRequest request,
        CancellationToken cancellationToken);

    /// <summary>Deletes a Created or InProgress task together with all of its relationships.</summary>
    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
