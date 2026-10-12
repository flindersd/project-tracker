using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Persistence;

public sealed class BoardDbContext : DbContext
{
    private readonly IBoardScopeAccessor boardScopeAccessor;

    public BoardDbContext(DbContextOptions<BoardDbContext> options, IBoardScopeAccessor boardScopeAccessor)
        : base(options)
    {
        this.boardScopeAccessor = boardScopeAccessor;
    }

    public DbSet<global::ProjectTracker.Api.Modules.Board.Domain.Board> Boards => Set<global::ProjectTracker.Api.Modules.Board.Domain.Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<TaskSubStage> TaskSubStages => Set<TaskSubStage>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardDbContext).Assembly);

        modelBuilder.Entity<global::ProjectTracker.Api.Modules.Board.Domain.Board>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<BoardColumn>().HasQueryFilter(e => e.DeletedAt == null && e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<Initiative>().HasQueryFilter(e => e.DeletedAt == null && e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<TaskItem>().HasQueryFilter(e => e.DeletedAt == null && e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<Tag>().HasQueryFilter(e => e.DeletedAt == null && e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<TaskSubStage>().HasQueryFilter(e => e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<TaskAssignee>().HasQueryFilter(e => e.BoardId == boardScopeAccessor.CurrentBoardId);
        modelBuilder.Entity<TaskTag>().HasQueryFilter(e => e.BoardId == boardScopeAccessor.CurrentBoardId);
    }
}
