using FluentValidation;
using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.Validation;

public sealed class ReplaceRelatedTasksRequestValidator : AbstractValidator<ReplaceRelatedTasksRequest>
{
    public ReplaceRelatedTasksRequestValidator()
    {
        RuleFor(x => x.RelatedTaskIds)
            .NotNull();
    }
}
