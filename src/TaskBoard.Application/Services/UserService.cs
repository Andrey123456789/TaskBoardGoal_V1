using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Results;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Services;

internal sealed class UserService(
    IUserRepository users,
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    IValidator<SaveUserRequest> validator,
    TimeProvider timeProvider,
    ILogger<UserService> logger)
    : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        var ordinaryUsers = await users.ListOrdinaryAsync(cancellationToken);

        return ordinaryUsers.Select(ToDto).ToList();
    }

    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await users.GetOrdinaryByIdAsync(id, cancellationToken);

        return user is null ? UserErrors.NotFound(id) : ToDto(user);
    }

    public async Task<Result<UserDto>> CreateAsync(
        SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = InputText.Required(request.Name);
        if (User.IsReservedName(name))
        {
            return UserErrors.ReservedName;
        }

        var email = InputText.Required(request.Email);
        if (await users.EmailExistsAsync(email, excludedUserId: null, cancellationToken))
        {
            return UserErrors.DuplicateEmail;
        }

        var user = User.Create(name, email, timeProvider.GetUtcNow());

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var user = await users.GetOrdinaryByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        var name = InputText.Required(request.Name);
        if (User.IsReservedName(name))
        {
            return UserErrors.ReservedName;
        }

        var email = InputText.Required(request.Email);
        if (await users.EmailExistsAsync(email, excludedUserId: id, cancellationToken))
        {
            return UserErrors.DuplicateEmail;
        }

        user.Update(name, email);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await users.GetOrdinaryByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        var assignedTasks = await tasks.ListAssignedToAsync(id, cancellationToken);
        foreach (var task in assignedTasks)
        {
            task.ReassignToDeletedUser();
        }

        users.Remove(user);

        // Reassignment and deletion are committed together in a single unit of work.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted; {TaskCount} tasks reassigned to the Deleted User",
            id,
            assignedTasks.Count);

        return Result.Success();
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}
