using TaskBoard.Application.Results;

namespace TaskBoard.Application.Services;

public static class ProjectErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("project.not_found", $"Project '{id}' was not found.");

    public static readonly Error DuplicateName =
        Error.Conflict("project.duplicate_name", "A project with this name already exists.");

    public static readonly Error HasTasks =
        Error.Conflict("project.has_tasks", "A project that contains tasks cannot be deleted.");
}
