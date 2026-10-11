using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Services;

public static class InitiativeMoveService
{
    public static InitiativeMoveResult Move(
        Initiative initiative,
        IReadOnlyList<TaskItem> tasks,
        BoardColumn destinationColumn,
        IReadOnlyList<TaskSubStage> destinationSubStagesOrderedBySortOrder)
    {
        if (destinationColumn.Type == ColumnType.InitiativeTask && destinationSubStagesOrderedBySortOrder.Count == 0)
        {
            throw new BoardStructuralViolationException();
        }

        var movedInitiative = CloneInitiative(initiative);
        movedInitiative.ColumnId = destinationColumn.Id;

        var movedTasks = tasks.Select(task => CloneTask(task)).ToList();

        if (destinationColumn.Type == ColumnType.InitiativeOnly)
        {
            foreach (var task in movedTasks)
            {
                task.DormantSubStageId ??= task.TaskSubStageId;
                task.TaskSubStageId = null;
                task.InitiativeId = movedInitiative.Id;
            }

            return new InitiativeMoveResult(movedInitiative, movedTasks);
        }

        var firstDestinationSubStage = destinationSubStagesOrderedBySortOrder.OrderBy(x => x.SortOrder, StringComparer.Ordinal).First();

        foreach (var task in movedTasks)
        {
            task.InitiativeId = movedInitiative.Id;

            var sourceTaskSubStage = ResolveTaskSubStage(task.TaskSubStageId);
            var isInactiveCarryOver = sourceTaskSubStage?.IsInactive == true;

            if (isInactiveCarryOver)
            {
                continue;
            }

            task.TaskSubStageId = firstDestinationSubStage.Id;
        }

        return new InitiativeMoveResult(movedInitiative, movedTasks);
    }

    private static TaskSubStage? ResolveTaskSubStage(Guid? taskSubStageId)
    {
        return taskSubStageId is Guid id && TaskSubStage.TryGetRegistered(id, out var subStage)
            ? subStage
            : null;
    }

    private static Initiative CloneInitiative(Initiative source) => new()
    {
        Id = source.Id,
        BoardId = source.BoardId,
        ColumnId = source.ColumnId,
        Title = source.Title,
        Description = source.Description,
        SortOrder = source.SortOrder,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        DeletedAt = source.DeletedAt,
    };

    private static TaskItem CloneTask(TaskItem source) => new()
    {
        Id = source.Id,
        BoardId = source.BoardId,
        CreatedBy = source.CreatedBy,
        InitiativeId = source.InitiativeId,
        TaskSubStageId = source.TaskSubStageId,
        DormantSubStageId = source.DormantSubStageId,
        Title = source.Title,
        Description = source.Description,
        DueDate = source.DueDate,
        Priority = source.Priority,
        SortOrder = source.SortOrder,
        RowVersion = source.RowVersion,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        DeletedAt = source.DeletedAt,
    };
}

public sealed record InitiativeMoveResult(Initiative Initiative, IReadOnlyList<TaskItem> Tasks);
