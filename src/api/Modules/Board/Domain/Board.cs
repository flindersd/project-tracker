namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class Board : IBoardScoped, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BoardId => Id;
    public string Name { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
