namespace TaskBoard.Application.DTOs.Projects;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);
