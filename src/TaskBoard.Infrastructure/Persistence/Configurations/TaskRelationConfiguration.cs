using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class TaskRelationConfiguration : IEntityTypeConfiguration<TaskRelation>
{
    public void Configure(EntityTypeBuilder<TaskRelation> builder)
    {
        builder.ToTable("TaskRelations", table =>
            table.HasCheckConstraint(
                "CK_TaskRelations_NoSelfRelation",
                "[FirstTaskId] <> [SecondTaskId]"));

        // The domain stores each symmetric pair in canonical order, so the composite key
        // guarantees that a relationship between two tasks exists at most once.
        builder.HasKey(x => new { x.FirstTaskId, x.SecondTaskId });

        // SQL Server does not allow two cascading paths to the same table; relationship rows are
        // removed explicitly together with the task.
        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => x.FirstTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => x.SecondTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SecondTaskId);
    }
}
