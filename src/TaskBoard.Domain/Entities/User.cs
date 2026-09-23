namespace TaskBoard.Domain.Entities;

/// <summary>
/// A person tasks can be assigned to. Users are ordinary application data, not identities.
/// </summary>
public sealed class User
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 254;

    /// <summary>
    /// Well-known identifier of the system <c>Deleted User</c>. Tasks whose assignee was deleted
    /// are reassigned to this record. It is not an ordinary user and cannot be assigned manually.
    /// </summary>
    public static readonly Guid DeletedUserId = new("00000000-0000-0000-0000-000000000001");

    public const string DeletedUserName = "Deleted User";

    /// <summary>Reserved address (RFC 2606 <c>.invalid</c> TLD) that cannot belong to a real mailbox.</summary>
    public const string DeletedUserEmail = "deleted-user@taskboard.invalid";

    private User()
    {
    }

    private User(Guid id, string name, string email, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsDeletedUser => Id == DeletedUserId;

    /// <summary>
    /// The system user's name is reserved so that no ordinary user can pose as the Deleted User.
    /// </summary>
    public static bool IsReservedName(string name) =>
        string.Equals(name.Trim(), DeletedUserName, StringComparison.OrdinalIgnoreCase);

    public static User Create(string name, string email, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        EnsureNotReserved(name);

        return new User(Guid.NewGuid(), name, email, createdAt);
    }

    public void Update(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        EnsureNotReserved(name);

        if (IsDeletedUser)
        {
            throw new InvalidOperationException("The system Deleted User cannot be edited.");
        }

        Name = name;
        Email = email;
    }

    private static void EnsureNotReserved(string name)
    {
        if (IsReservedName(name))
        {
            throw new ArgumentException($"The name '{DeletedUserName}' is reserved.", nameof(name));
        }
    }
}
