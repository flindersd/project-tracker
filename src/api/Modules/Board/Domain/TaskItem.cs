namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed record class TaskItem : IBoardScoped, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid InitiativeId { get; set; }
    public Guid? TaskSubStageId { get; set; }
    public Guid? DormantSubStageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public string SortOrder { get; set; } = string.Empty;
    public long RowVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
