using FluentValidation;
using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.Validation;

public sealed class TaskFilterValidator : AbstractValidator<TaskFilter>
{
    public TaskFilterValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status is not null);
    }
}
