using ProjectTracker.Api.Modules.Board.Domain;
using ProjectTracker.Api.Modules.Board.Services;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class InitiativeMoveServiceTests
{
    // Given an initiative in column A (initiative_task) with a task whose active sub-stage is
    // active (not inactive), and column B (initiative_task) has sub-stages ordered [S1, S2],
    // When the initiative is moved to column B,
    // Then the task's active taskSubStageId becomes S1.Id.
    [Fact]
    public void Move_reassigns_active_substage_task_to_destination_first_substage()
    {
        var boardId = Guid.NewGuid();
        var columnA = NewColumn(boardId, ColumnType.InitiativeTask);
        var s1 = NewSubStage(boardId, columnA.Id, sortOrder: "a");
        var s2 = NewSubStage(boardId, columnA.Id, sortOrder: "b");
        var columnB = NewColumn(boardId, ColumnType.InitiativeTask);
        var destS1 = NewSubStage(boardId, columnB.Id, sortOrder: "a");
        var destS2 = NewSubStage(boardId, columnB.Id, sortOrder: "b");

        var initiative = NewInitiative(boardId, columnA.Id);
        var task = NewTask(boardId, initiative.Id, activeSubStageId: s1.Id);

        var result = InitiativeMoveService.Move(
            initiative,
            tasks: [task],
            destinationColumn: columnB,
            destinationSubStagesOrderedBySortOrder: [destS1, destS2]);

        Assert.Equal(destS1.Id, result.Tasks.Single().TaskSubStageId);
    }

    // Given an initiative in column A with a task whose active sub-stage isInactive = true,
    // and column B (initiative_task) has sub-stages ordered [S1, S2],
    // When the initiative is moved to column B,
    // Then the task's active taskSubStageId is unchanged, and its initiative link is updated.
    [Fact]
    public void Move_excludes_inactive_substage_task_from_reassignment_carry_over()
    {
        var boardId = Guid.NewGuid();
        var columnA = NewColumn(boardId, ColumnType.InitiativeTask);
        var inactiveSubStage = NewSubStage(boardId, columnA.Id, sortOrder: "a", isInactive: true);
        var columnB = NewColumn(boardId, ColumnType.InitiativeTask);
        var destS1 = NewSubStage(boardId, columnB.Id, sortOrder: "a");
        var destS2 = NewSubStage(boardId, columnB.Id, sortOrder: "b");

        var initiative = NewInitiative(boardId, columnA.Id);
        var task = NewTask(boardId, initiative.Id, activeSubStageId: inactiveSubStage.Id);

        var result = InitiativeMoveService.Move(
            initiative,
            tasks: [task],
            destinationColumn: columnB,
            destinationSubStagesOrderedBySortOrder: [destS1, destS2],
            resolveTaskSubStage: id => id == inactiveSubStage.Id ? inactiveSubStage : null);

        var movedTask = result.Tasks.Single();
        Assert.Equal(inactiveSubStage.Id, movedTask.TaskSubStageId);
        Assert.Equal(result.Initiative.Id, movedTask.InitiativeId);
        Assert.Equal(columnB.Id, result.Initiative.ColumnId);
    }

    // Given column B is initiative_task type with zero configured sub-stages,
    // When an initiative is moved into column B,
    // Then the move is rejected with a structural-violation error and the initiative remains
    // in its original column.
    [Fact]
    public void Move_is_rejected_when_destination_has_no_substages()
    {
        var boardId = Guid.NewGuid();
        var columnA = NewColumn(boardId, ColumnType.InitiativeTask);
        var columnB = NewColumn(boardId, ColumnType.InitiativeTask);
        var initiative = NewInitiative(boardId, columnA.Id);

        Assert.Throws<BoardStructuralViolationException>(() =>
            InitiativeMoveService.Move(
                initiative,
                tasks: [],
                destinationColumn: columnB,
                destinationSubStagesOrderedBySortOrder: []));
    }

    // Given an initiative in column A with two tasks — one with an active (non-inactive)
    // sub-stage, one with an inactive sub-stage — and column C is initiative_only,
    // When the initiative is moved to column C,
    // Then both tasks go dormant: dormantSubStageId set, active taskSubStageId null.
    // (§3.3 resolution 2 — the carry-over exclusion does NOT apply to moves into initiative_only.)
    [Fact]
    public void Move_into_initiative_only_column_sends_all_tasks_dormant_including_inactive_substage_tasks()
    {
        var boardId = Guid.NewGuid();
        var columnA = NewColumn(boardId, ColumnType.InitiativeTask);
        var activeSubStage = NewSubStage(boardId, columnA.Id, sortOrder: "a");
        var inactiveSubStage = NewSubStage(boardId, columnA.Id, sortOrder: "b", isInactive: true);
        var columnC = NewColumn(boardId, ColumnType.InitiativeOnly);

        var initiative = NewInitiative(boardId, columnA.Id);
        var taskWithActiveSubStage = NewTask(boardId, initiative.Id, activeSubStageId: activeSubStage.Id);
        var taskWithInactiveSubStage = NewTask(boardId, initiative.Id, activeSubStageId: inactiveSubStage.Id);

        var result = InitiativeMoveService.Move(
            initiative,
            tasks: [taskWithActiveSubStage, taskWithInactiveSubStage],
            destinationColumn: columnC,
            destinationSubStagesOrderedBySortOrder: []);

        Assert.All(result.Tasks, t => Assert.Null(t.TaskSubStageId));
        var byId = result.Tasks.ToDictionary(t => t.Id);
        Assert.Equal(activeSubStage.Id, byId[taskWithActiveSubStage.Id].DormantSubStageId);
        Assert.Equal(inactiveSubStage.Id, byId[taskWithInactiveSubStage.Id].DormantSubStageId);
    }

    // Given an initiative currently in an initiative_only column with tasks holding only
    // dormantSubStageId values, and destination column D (initiative_task) has sub-stages
    // ordered [S1, S2],
    // When the initiative is moved to column D,
    // Then every task's active taskSubStageId becomes S1.Id, and dormantSubStageId is
    // untouched (non-load-bearing / forensic only).
    [Fact]
    public void Move_from_initiative_only_resets_every_task_to_destination_first_substage()
    {
        var boardId = Guid.NewGuid();
        var dormantOnlyColumn = NewColumn(boardId, ColumnType.InitiativeOnly);
        var columnD = NewColumn(boardId, ColumnType.InitiativeTask);
        var destS1 = NewSubStage(boardId, columnD.Id, sortOrder: "a");
        var destS2 = NewSubStage(boardId, columnD.Id, sortOrder: "b");

        var initiative = NewInitiative(boardId, dormantOnlyColumn.Id);
        var priorDormantId = Guid.NewGuid();
        var task = NewTask(boardId, initiative.Id, activeSubStageId: null) with { DormantSubStageId = priorDormantId };

        var result = InitiativeMoveService.Move(
            initiative,
            tasks: [task],
            destinationColumn: columnD,
            destinationSubStagesOrderedBySortOrder: [destS1, destS2]);

        var movedTask = result.Tasks.Single();
        Assert.Equal(destS1.Id, movedTask.TaskSubStageId);
        Assert.Equal(priorDormantId, movedTask.DormantSubStageId);
    }

    private static BoardColumn NewColumn(Guid boardId, ColumnType type) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = "Column",
            SortOrder = "a",
            Type = type,
        };

    private static TaskSubStage NewSubStage(Guid boardId, Guid parentColumnId, string sortOrder, bool isInactive = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            ParentColumnId = parentColumnId,
            Name = "Sub-stage",
            SortOrder = sortOrder,
            IsInactive = isInactive,
        };

    private static Initiative NewInitiative(Guid boardId, Guid columnId) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            ColumnId = columnId,
            Title = "Initiative",
            SortOrder = "a",
        };

    private static TaskItem NewTask(Guid boardId, Guid initiativeId, Guid? activeSubStageId) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            InitiativeId = initiativeId,
            TaskSubStageId = activeSubStageId,
            DormantSubStageId = null,
            Title = "Task",
            Priority = TaskPriority.Medium,
            SortOrder = "a",
        };
}
