namespace TaskBoard.Application.DTOs.Users;

/// <summary>
/// Request body for creating or updating an ordinary user.
/// </summary>
public sealed record SaveUserRequest(
    string Name,
    string Email);
