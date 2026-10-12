using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.ToTable("task_tags");

        builder.HasKey(x => new { x.BoardId, x.TaskId, x.TagId });

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(x => x.TagId)
            .HasColumnName("tag_id")
            .IsRequired();

        builder.HasIndex(x => new { x.BoardId, x.TaskId, x.TagId })
            .IsUnique();

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.TaskId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.TagId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
