namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class Tag : IBoardScoped, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
