using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("board_columns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("varchar(64)")
            .UseCollation("C")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("column_type")
            .HasConversion(new ValueConverter<ColumnType, string>(
                value => value == ColumnType.InitiativeOnly ? "initiative_only" : "initiative_task",
                value => value == "initiative_only" ? ColumnType.InitiativeOnly : ColumnType.InitiativeTask))
            .HasColumnType("varchar(32)")
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

        builder.HasIndex(x => new { x.BoardId, x.SortOrder })
            .IsUnique();

        builder.ToTable(tb => tb.HasCheckConstraint("ck_board_columns_column_type", "column_type IN ('initiative_only', 'initiative_task')"));

        builder.HasOne<global::ProjectTracker.Api.Modules.Board.Domain.Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
