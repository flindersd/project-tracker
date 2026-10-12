namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class TaskAssignee : IBoardScoped
{
    public Guid BoardId { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid AssignedBy { get; set; }
}
