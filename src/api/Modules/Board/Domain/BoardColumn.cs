namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class BoardColumn : IBoardScoped, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SortOrder { get; set; } = string.Empty;
    public ColumnType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
