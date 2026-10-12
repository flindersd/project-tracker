using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("task_assignees");

        builder.HasKey(x => new { x.BoardId, x.TaskId, x.UserId });

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        builder.Property(x => x.AssignedBy)
            .HasColumnName("assigned_by")
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_task_assignees_user_id");

        builder.HasIndex(x => new { x.BoardId, x.TaskId, x.UserId })
            .IsUnique();

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.TaskId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
