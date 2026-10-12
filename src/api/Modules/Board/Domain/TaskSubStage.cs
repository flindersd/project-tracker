using ProjectTracker.Api.Shared;

namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class TaskSubStage : IBoardScoped
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ParentColumnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SortOrder { get; set; } = string.Empty;
    public bool IsInactive { get; set; }

    public static TaskSubStage Create(BoardColumn parent, string name, string sortOrder)
    {
        if (parent.Type != ColumnType.InitiativeTask)
        {
            throw new BoardStructuralViolationException();
        }

        return new TaskSubStage
        {
            Id = UuidV7.NewGuid(),
            BoardId = parent.BoardId,
            ParentColumnId = parent.Id,
            Name = name,
            SortOrder = sortOrder,
        };
    }

    public static void MarkInactive(TaskSubStage subStage)
    {
        subStage.IsInactive = true;
    }

    public static void SetInactive(TaskSubStage subStage) => MarkInactive(subStage);

    public static void ClearInactive(TaskSubStage subStage, IReadOnlyCollection<Initiative> initiatives, IReadOnlyCollection<TaskItem> tasks)
    {
        var initiativesById = initiatives.ToDictionary(x => x.Id);
        var retainedTaskExistsOutsideCurrentColumn = tasks.Any(task =>
            task.TaskSubStageId == subStage.Id
            && initiativesById.TryGetValue(task.InitiativeId, out var initiative)
            && initiative.ColumnId != subStage.ParentColumnId);

        if (retainedTaskExistsOutsideCurrentColumn)
        {
            throw new BoardStructuralViolationException();
        }

        subStage.IsInactive = false;
    }
}
