using TaskBoard.Application.Results;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Services;

public static class UserErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("user.not_found", $"User '{id}' was not found.");

    public static readonly Error DuplicateEmail =
        Error.Conflict("user.duplicate_email", "A user with this email already exists.");

    public static readonly Error ReservedName =
        Error.Unprocessable(
            "user.reserved_name",
            $"The name '{User.DeletedUserName}' is reserved for the system user.");
}
