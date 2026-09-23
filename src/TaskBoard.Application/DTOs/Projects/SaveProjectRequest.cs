namespace TaskBoard.Application.DTOs.Projects;

/// <summary>
/// Request body for creating or updating a project.
/// </summary>
public sealed record SaveProjectRequest(
    string Name,
    string? Description);
