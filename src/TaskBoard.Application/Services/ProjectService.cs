using FluentValidation;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.Results;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Services;

internal sealed class ProjectService(
    IProjectRepository projects,
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    IValidator<SaveProjectRequest> validator,
    TimeProvider timeProvider)
    : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        var allProjects = await projects.ListAsync(cancellationToken);

        return allProjects.Select(ToDto).ToList();
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);

        return project is null ? ProjectErrors.NotFound(id) : ToDto(project);
    }

    public async Task<Result<ProjectDto>> CreateAsync(
        SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = InputText.Required(request.Name);
        if (await projects.NameExistsAsync(name, excludedProjectId: null, cancellationToken))
        {
            return ProjectErrors.DuplicateName;
        }

        var project = Project.Create(
            name,
            InputText.Optional(request.Description),
            timeProvider.GetUtcNow());

        projects.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(project);
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var project = await projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ProjectErrors.NotFound(id);
        }

        var name = InputText.Required(request.Name);
        if (await projects.NameExistsAsync(name, excludedProjectId: id, cancellationToken))
        {
            return ProjectErrors.DuplicateName;
        }

        project.Update(name, InputText.Optional(request.Description));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ProjectErrors.NotFound(id);
        }

        if (await tasks.AnyInProjectAsync(id, cancellationToken))
        {
            return ProjectErrors.HasTasks;
        }

        projects.Remove(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static ProjectDto ToDto(Project project) =>
        new(project.Id, project.Name, project.Description, project.CreatedAt);
}
