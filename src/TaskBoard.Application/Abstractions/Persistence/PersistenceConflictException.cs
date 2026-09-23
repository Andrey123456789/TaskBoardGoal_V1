namespace TaskBoard.Application.Abstractions.Persistence;

/// <summary>
/// Raised by the persistence layer when saving fails because the stored data changed concurrently
/// (for example, a uniqueness or referential constraint was violated after the use case validated
/// its input). Use cases check these rules up front, so this only occurs in race conditions.
/// </summary>
public sealed class PersistenceConflictException(string message, Exception innerException)
    : Exception(message, innerException);
