using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(x => x.InitiativeId)
            .HasColumnName("initiative_id")
            .IsRequired();

        builder.Property(x => x.TaskSubStageId)
            .HasColumnName("task_sub_stage_id");

        builder.Property(x => x.DormantSubStageId)
            .HasColumnName("dormant_sub_stage_id");

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date");

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasColumnType("varchar(16)")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("varchar(64)")
            .UseCollation("C")
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1L)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(x => new { x.BoardId, x.Id })
            .IsUnique();

        builder.HasIndex(x => new { x.BoardId, x.InitiativeId })
            .HasDatabaseName("ix_tasks_board_id_initiative_id");

        builder.HasIndex(x => new { x.BoardId, x.TaskSubStageId })
            .HasDatabaseName("ix_tasks_board_id_task_sub_stage_id");

        builder.HasIndex(x => new { x.BoardId, x.InitiativeId, x.TaskSubStageId })
            .IsUnique();

        builder.ToTable(tb => tb.HasCheckConstraint("ck_tasks_priority", "priority IN ('Low', 'Medium', 'High', 'Urgent')"));

        builder.HasOne<Initiative>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.InitiativeId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaskSubStage>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.TaskSubStageId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaskSubStage>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.DormantSubStageId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.SetNull);
    }
}
