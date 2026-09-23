using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

public sealed record TaskDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    ProjectSummaryDto Project,
    AssigneeDto? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RelatedTaskDto> RelatedTasks);
