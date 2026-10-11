using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class TaskSubStageConfiguration : IEntityTypeConfiguration<TaskSubStage>
{
    public void Configure(EntityTypeBuilder<TaskSubStage> builder)
    {
        builder.ToTable("task_sub_stages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.ParentColumnId)
            .HasColumnName("parent_column_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("varchar(64)")
            .UseCollation("C")
            .IsRequired();

        builder.Property(x => x.IsInactive)
            .HasColumnName("is_inactive")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.BoardId, x.Id })
            .IsUnique();

        builder.HasIndex(x => new { x.BoardId, x.ParentColumnId, x.SortOrder })
            .IsUnique();

        builder.HasOne<BoardColumn>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.ParentColumnId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
