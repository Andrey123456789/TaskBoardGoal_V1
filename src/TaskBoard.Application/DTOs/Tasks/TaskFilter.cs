using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// Optional, combinable task-list filters.
/// </summary>
public sealed record TaskFilter(
    Guid? ProjectId,
    Guid? AssigneeId,
    TaskItemStatus? Status);
