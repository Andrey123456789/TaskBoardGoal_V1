using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskSummaryDto>> ListSummariesAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Tasks.AsQueryable();

        if (filter.ProjectId is { } projectId)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (filter.AssigneeId is { } assigneeId)
        {
            query = query.Where(x => x.AssigneeId == assigneeId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(x => x.Status == status);
        }

        // Project and assignee names are joined in the same query so the dashboard needs a
        // single request regardless of how many projects and users are involved.
        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new TaskSummaryDto(
                x.Id,
                x.Title,
                x.Status,
                new ProjectSummaryDto(x.Project!.Id, x.Project.Name),
                x.AssigneeId == null
                    ? null
                    : new AssigneeDto(x.Assignee!.Id, x.Assignee.Name, x.Assignee.Id == User.DeletedUserId),
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDetailsDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                Project = new ProjectSummaryDto(x.Project!.Id, x.Project.Name),
                Assignee = x.AssigneeId == null
                    ? null
                    : new AssigneeDto(x.Assignee!.Id, x.Assignee.Name, x.Assignee.Id == User.DeletedUserId),
                x.CreatedAt,
                x.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return null;
        }

        var relatedTaskIds = db.TaskRelations
            .Where(x => x.FirstTaskId == id)
            .Select(x => x.SecondTaskId)
            .Concat(db.TaskRelations
                .Where(x => x.SecondTaskId == id)
                .Select(x => x.FirstTaskId));

        var relatedTasks = await db.Tasks
            .Where(x => relatedTaskIds.Contains(x.Id))
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .Select(x => new RelatedTaskDto(
                x.Id,
                x.Title,
                x.Status,
                new ProjectSummaryDto(x.Project!.Id, x.Project.Name)))
            .ToListAsync(cancellationToken);

        return new TaskDetailsDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Project,
            task.Assignee,
            task.CreatedAt,
            task.UpdatedAt,
            relatedTasks);
    }

    public Task<TaskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Tasks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Tasks.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var existingIds = await db.Tasks
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    public async Task<IReadOnlyList<TaskItem>> ListAssignedToAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await db.Tasks
            .Where(x => x.AssigneeId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyInProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return db.Tasks.AnyAsync(x => x.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskRelation>> ListRelationsAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return await db.TaskRelations
            .Where(x => x.FirstTaskId == taskId || x.SecondTaskId == taskId)
            .ToListAsync(cancellationToken);
    }

    public void Add(TaskItem task) => db.Tasks.Add(task);

    public void Remove(TaskItem task) => db.Tasks.Remove(task);

    public void AddRelation(TaskRelation relation) => db.TaskRelations.Add(relation);

    public void RemoveRelation(TaskRelation relation) => db.TaskRelations.Remove(relation);
}
