namespace TaskBoard.Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);
