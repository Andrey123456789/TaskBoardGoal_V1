using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly DateTimeOffset DeletedUserCreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(User.EmailMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        // The system Deleted User is part of the schema's reference data, so it exists in every
        // environment independently of Development demo seeding.
        builder.HasData(new
        {
            Id = User.DeletedUserId,
            Name = User.DeletedUserName,
            Email = User.DeletedUserEmail,
            CreatedAt = DeletedUserCreatedAt,
        });
    }
}
