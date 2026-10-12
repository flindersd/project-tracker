using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence.Configurations;

public sealed class InitiativeConfiguration : IEntityTypeConfiguration<Initiative>
{
    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.ToTable("initiatives");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BoardId)
            .HasColumnName("board_id")
            .IsRequired();

        builder.Property(x => x.ColumnId)
            .HasColumnName("column_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("varchar(64)")
            .UseCollation("C")
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

        builder.HasIndex(x => new { x.BoardId, x.ColumnId, x.SortOrder })
            .IsUnique();

        builder.HasOne<BoardColumn>()
            .WithMany()
            .HasForeignKey(x => new { x.BoardId, x.ColumnId })
            .HasPrincipalKey(x => new { x.BoardId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
