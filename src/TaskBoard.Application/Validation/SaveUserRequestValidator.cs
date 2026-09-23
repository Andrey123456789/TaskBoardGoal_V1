using FluentValidation;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Validation;

public sealed class SaveUserRequestValidator : AbstractValidator<SaveUserRequest>
{
    public SaveUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(User.NameMaxLength);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(User.EmailMaxLength)
            .Must(EmailAddress.IsValid)
            .WithMessage("'Email' must be a valid email address.");
    }
}
