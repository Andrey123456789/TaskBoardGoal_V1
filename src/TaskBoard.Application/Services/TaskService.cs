using FluentValidation;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Results;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Services;

internal sealed class TaskService(
    ITaskRepository tasks,
    IProjectRepository projects,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IValidator<TaskFilter> filterValidator,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<ChangeTaskStatusRequest> statusValidator,
    IValidator<ReplaceRelatedTasksRequest> relatedTasksValidator,
    TimeProvider timeProvider)
    : ITaskService
{
    public async Task<Result<IReadOnlyList<TaskSummaryDto>>> ListAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var validation = await filterValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var summaries = await tasks.ListSummariesAsync(filter, cancellationToken);

        return Result<IReadOnlyList<TaskSummaryDto>>.Success(summaries);
    }

    public async Task<Result<TaskDetailsDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var details = await tasks.GetDetailsAsync(id, cancellationToken);

        return details is null ? TaskErrors.NotFound(id) : details;
    }

    public async Task<Result<TaskDetailsDto>> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        if (!await projects.ExistsAsync(request.ProjectId, cancellationToken))
        {
            return TaskErrors.ProjectNotFound(request.ProjectId);
        }

        if (request.AssigneeId is { } assigneeId &&
            await CheckAssignableAsync(assigneeId, cancellationToken) is { } assigneeError)
        {
            return assigneeError;
        }

        var relatedTaskIds = (request.RelatedTaskIds ?? []).Distinct().ToList();
        if (await CheckTasksExistAsync(relatedTaskIds, cancellationToken) is { } relatedError)
        {
            return relatedError;
        }

        var task = TaskItem.Create(
            InputText.Required(request.Title),
            InputText.Optional(request.Description),
            request.ProjectId,
            request.AssigneeId,
            timeProvider.GetUtcNow());

        tasks.Add(task);
        foreach (var relatedTaskId in relatedTaskIds)
        {
            tasks.AddRelation(TaskRelation.Create(task.Id, relatedTaskId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await tasks.GetDetailsAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Task '{task.Id}' could not be read after it was created.");
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        // A state conflict takes precedence over problems with the submitted values.
        if (task.IsLocked)
        {
            return TaskErrors.Locked(task.Status);
        }

        if (request.AssigneeId is { } assigneeId &&
            await CheckAssignableAsync(assigneeId, cancellationToken) is { } assigneeError)
        {
            return assigneeError;
        }

        var outcome = task.UpdateDetails(
            InputText.Required(request.Title),
            InputText.Optional(request.Description),
            request.AssigneeId,
            timeProvider.GetUtcNow());

        if (outcome != TaskUpdateOutcome.Updated)
        {
            return outcome switch
            {
                TaskUpdateOutcome.TaskLocked => TaskErrors.Locked(task.Status),
                TaskUpdateOutcome.AssigneeRequired => TaskErrors.AssigneeRequired,
                TaskUpdateOutcome.DeletedUserNotAssignable => TaskErrors.DeletedUserNotAssignable,
                _ => throw new InvalidOperationException($"Unexpected task update outcome '{outcome}'."),
            };
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(
        Guid id,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        var currentStatus = task.Status;
        var targetStatus = request.Status!.Value;

        var outcome = task.TransitionTo(targetStatus, timeProvider.GetUtcNow());

        if (outcome != TaskTransitionOutcome.Transitioned)
        {
            return outcome switch
            {
                TaskTransitionOutcome.NotAllowed => TaskErrors.InvalidTransition(currentStatus, targetStatus),
                TaskTransitionOutcome.ActiveAssigneeRequired => TaskErrors.ActiveAssigneeRequired,
                _ => throw new InvalidOperationException($"Unexpected task transition outcome '{outcome}'."),
            };
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReplaceRelatedTasksAsync(
        Guid id,
        ReplaceRelatedTasksRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await relatedTasksValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        if (!await tasks.ExistsAsync(id, cancellationToken))
        {
            return TaskErrors.NotFound(id);
        }

        var desiredIds = request.RelatedTaskIds.ToHashSet();

        if (desiredIds.Contains(id))
        {
            return TaskErrors.SelfRelation;
        }

        if (await CheckTasksExistAsync(desiredIds, cancellationToken) is { } relatedError)
        {
            return relatedError;
        }

        var currentRelations = await tasks.ListRelationsAsync(id, cancellationToken);
        var currentIds = new HashSet<Guid>();

        foreach (var relation in currentRelations)
        {
            var otherId = relation.GetOtherTaskId(id);

            if (desiredIds.Contains(otherId))
            {
                currentIds.Add(otherId);
            }
            else
            {
                tasks.RemoveRelation(relation);
            }
        }

        foreach (var missingId in desiredIds.Where(desiredId => !currentIds.Contains(desiredId)))
        {
            tasks.AddRelation(TaskRelation.Create(id, missingId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        if (!task.CanBeDeleted)
        {
            return TaskErrors.DeletionNotAllowed(task.Status);
        }

        var relations = await tasks.ListRelationsAsync(id, cancellationToken);
        foreach (var relation in relations)
        {
            tasks.RemoveRelation(relation);
        }

        tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Error?> CheckAssignableAsync(
        Guid assigneeId,
        CancellationToken cancellationToken)
    {
        if (assigneeId == User.DeletedUserId)
        {
            return TaskErrors.DeletedUserNotAssignable;
        }

        return await users.OrdinaryUserExistsAsync(assigneeId, cancellationToken)
            ? null
            : TaskErrors.AssigneeNotFound(assigneeId);
    }

    private async Task<Error?> CheckTasksExistAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return null;
        }

        var existingIds = await tasks.GetExistingIdsAsync(taskIds, cancellationToken);
        var missingIds = taskIds.Where(taskId => !existingIds.Contains(taskId)).ToList();

        return missingIds.Count == 0 ? null : TaskErrors.RelatedTasksNotFound(missingIds);
    }
}
