using ProjectTracker.Api.Modules.Board.Domain;
using ProjectTracker.Api.Modules.Board.Services;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class ColumnTypeChangeServiceTests
{
    // Given a column of type initiative_task with sub-stages [S1, S2] containing initiatives
    // whose tasks are split across both sub-stages,
    // When the column type is changed to initiative_only,
    // Then every task has dormantSubStageId set to its prior active sub-stage and active
    // taskSubStageId set to null, and the column's sub-stages no longer exist afterward
    // (§3.3 resolution 1).
    [Fact]
    public void ChangeToInitiativeOnly_applies_dormancy_and_removes_substages()
    {
        var boardId = Guid.NewGuid();
        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = "Column",
            SortOrder = "a",
            Type = ColumnType.InitiativeTask,
        };
        var s1 = new TaskSubStage { Id = Guid.NewGuid(), BoardId = boardId, ParentColumnId = column.Id, Name = "S1", SortOrder = "a" };
        var s2 = new TaskSubStage { Id = Guid.NewGuid(), BoardId = boardId, ParentColumnId = column.Id, Name = "S2", SortOrder = "b" };

        var initiativeId = Guid.NewGuid();
        var taskInS1 = new TaskItem { Id = Guid.NewGuid(), BoardId = boardId, InitiativeId = initiativeId, TaskSubStageId = s1.Id, Title = "T1", Priority = TaskPriority.Low, SortOrder = "a" };
        var taskInS2 = new TaskItem { Id = Guid.NewGuid(), BoardId = boardId, InitiativeId = initiativeId, TaskSubStageId = s2.Id, Title = "T2", Priority = TaskPriority.Low, SortOrder = "b" };

        var result = ColumnTypeChangeService.ChangeToInitiativeOnly(column, subStagesInColumn: [s1, s2], tasksInColumn: [taskInS1, taskInS2]);

        Assert.All(result.Tasks, t => Assert.Null(t.TaskSubStageId));
        var byId = result.Tasks.ToDictionary(t => t.Id);
        Assert.Equal(s1.Id, byId[taskInS1.Id].DormantSubStageId);
        Assert.Equal(s2.Id, byId[taskInS2.Id].DormantSubStageId);
        Assert.Empty(result.RemainingSubStages);
    }

    // Given a column of type initiative_only containing initiatives,
    // When the column type is changed to initiative_task,
    // Then the operation succeeds even with zero sub-stages, and every task belonging to
    // initiatives in that column has active taskSubStageId = null (left dormant) until an
    // explicit re-entry move (§3.3 resolution 3).
    [Fact]
    public void ChangeToInitiativeTask_is_permitted_with_zero_substages_and_leaves_tasks_dormant()
    {
        var boardId = Guid.NewGuid();
        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = "Column",
            SortOrder = "a",
            Type = ColumnType.InitiativeOnly,
        };
        var initiativeId = Guid.NewGuid();
        var dormantTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            InitiativeId = initiativeId,
            TaskSubStageId = null,
            DormantSubStageId = Guid.NewGuid(),
            Title = "T1",
            Priority = TaskPriority.Low,
            SortOrder = "a",
        };

        var result = ColumnTypeChangeService.ChangeToInitiativeTask(column, tasksInColumn: [dormantTask]);

        Assert.Equal(ColumnType.InitiativeTask, result.Column.Type);
        Assert.All(result.Tasks, t => Assert.Null(t.TaskSubStageId));
    }
}
