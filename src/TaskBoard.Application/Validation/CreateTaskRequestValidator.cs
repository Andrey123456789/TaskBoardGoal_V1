using FluentValidation;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(TaskItem.TitleMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength);

        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
