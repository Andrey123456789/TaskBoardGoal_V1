using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

public sealed record RelatedTaskDto(
    Guid Id,
    string Title,
    TaskItemStatus Status,
    ProjectSummaryDto Project);
