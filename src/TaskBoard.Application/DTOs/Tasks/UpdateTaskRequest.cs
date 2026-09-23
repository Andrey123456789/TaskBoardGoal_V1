namespace TaskBoard.Application.DTOs.Tasks;

/// <summary>
/// Request body for an ordinary task edit. The project, the status and the related tasks are not
/// part of this contract.
/// </summary>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? AssigneeId);
