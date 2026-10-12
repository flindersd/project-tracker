namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class TaskTag : IBoardScoped
{
    public Guid BoardId { get; set; }
    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }
}
