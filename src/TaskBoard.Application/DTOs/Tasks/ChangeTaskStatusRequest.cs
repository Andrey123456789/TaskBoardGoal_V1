using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.DTOs.Tasks;

public sealed record ChangeTaskStatusRequest(
    TaskItemStatus? Status);
