namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class TaskTag
{
    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }
}
