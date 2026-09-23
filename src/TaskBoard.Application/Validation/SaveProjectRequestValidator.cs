using FluentValidation;
using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

public sealed class SaveProjectRequestValidator : AbstractValidator<SaveProjectRequest>
{
    public SaveProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Project.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Project.DescriptionMaxLength);
    }
}
