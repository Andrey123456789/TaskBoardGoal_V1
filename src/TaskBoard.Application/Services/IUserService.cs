using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Results;

namespace TaskBoard.Application.Services;

/// <summary>
/// Management of ordinary users. The system Deleted User is never returned or modified here.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(
        CancellationToken cancellationToken);

    Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Result<UserDto>> CreateAsync(
        SaveUserRequest request,
        CancellationToken cancellationToken);

    Task<Result> UpdateAsync(
        Guid id,
        SaveUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the user and, in the same atomic operation, reassigns every task assigned to the
    /// user to the system Deleted User.
    /// </summary>
    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
