namespace TaskBoard.Domain.Entities;

public sealed class Project
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private Project()
    {
    }

    private Project(Guid id, string name, string? description, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Project Create(string name, string? description, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Project(Guid.NewGuid(), name, description, createdAt);
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
    }
}
