using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Services;

public static class ColumnTypeChangeService
{
    public static ColumnTypeChangeResult ChangeToInitiativeOnly(
        BoardColumn column,
        IReadOnlyList<TaskSubStage> subStagesInColumn,
        IReadOnlyList<TaskItem> tasksInColumn)
    {
        var tasks = tasksInColumn.Select(CloneTask).ToList();
        foreach (var task in tasks)
        {
            task.DormantSubStageId = task.TaskSubStageId;
            task.TaskSubStageId = null;
        }

        var changedColumn = CloneColumn(column);
        changedColumn.Type = ColumnType.InitiativeOnly;
        return new ColumnTypeChangeResult(changedColumn, tasks, Array.Empty<TaskSubStage>());
    }

    public static ColumnTypeChangeResult ChangeToInitiativeTask(
        BoardColumn column,
        IReadOnlyList<TaskItem> tasksInColumn)
    {
        var tasks = tasksInColumn.Select(CloneTask).ToList();
        foreach (var task in tasks)
        {
            task.TaskSubStageId = null;
        }

        var changedColumn = CloneColumn(column);
        changedColumn.Type = ColumnType.InitiativeTask;
        return new ColumnTypeChangeResult(changedColumn, tasks, Array.Empty<TaskSubStage>());
    }

    private static BoardColumn CloneColumn(BoardColumn source) => new()
    {
        Id = source.Id,
        BoardId = source.BoardId,
        Name = source.Name,
        SortOrder = source.SortOrder,
        Type = source.Type,
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

public sealed record ColumnTypeChangeResult(BoardColumn Column, IReadOnlyList<TaskItem> Tasks, IReadOnlyList<TaskSubStage> RemainingSubStages);
