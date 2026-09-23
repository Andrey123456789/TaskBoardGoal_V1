using FluentValidation;
using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.Validation;

public sealed class ChangeTaskStatusRequestValidator : AbstractValidator<ChangeTaskStatusRequest>
{
    public ChangeTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotNull()
            .IsInEnum();
    }
}
