using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

public sealed record TaskSummaryDto(
    Guid Id,
    string Title,
    TaskItemStatus Status,
    ProjectSummaryDto Project,
    AssigneeDto? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
