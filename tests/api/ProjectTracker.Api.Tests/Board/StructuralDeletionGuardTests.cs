using ProjectTracker.Api.Modules.Board.Domain;
using ProjectTracker.Api.Modules.Board.Services;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class StructuralDeletionGuardTests
{
    // Given a task sub-stage that currently has at least one task assigned,
    // When deletion of that sub-stage is attempted,
    // Then the deletion is rejected with a structural-violation error.
    [Fact]
    public void EnsureSubStageDeletable_rejects_substage_with_tasks()
    {
        var boardId = Guid.NewGuid();
        var subStage = new TaskSubStage { Id = Guid.NewGuid(), BoardId = boardId, ParentColumnId = Guid.NewGuid(), Name = "S1", SortOrder = "a" };
        var task = new TaskItem { Id = Guid.NewGuid(), BoardId = boardId, InitiativeId = Guid.NewGuid(), TaskSubStageId = subStage.Id, Title = "T", Priority = TaskPriority.Low, SortOrder = "a" };

        Assert.Throws<BoardStructuralViolationException>(() =>
            StructuralDeletionGuard.EnsureSubStageDeletable(subStage, tasksReferencingSubStage: [task]));
    }

    // Given a task sub-stage with zero tasks assigned,
    // When deletion of that sub-stage is attempted,
    // Then the deletion succeeds (no exception).
    [Fact]
    public void EnsureSubStageDeletable_allows_empty_substage()
    {
        var subStage = new TaskSubStage { Id = Guid.NewGuid(), BoardId = Guid.NewGuid(), ParentColumnId = Guid.NewGuid(), Name = "S1", SortOrder = "a" };

        var exception = Record.Exception(() =>
            StructuralDeletionGuard.EnsureSubStageDeletable(subStage, tasksReferencingSubStage: []));

        Assert.Null(exception);
    }

    // Given a column containing at least one initiative,
    // When deletion of that column is attempted,
    // Then the deletion is rejected with a structural-violation error.
    [Fact]
    public void EnsureColumnDeletable_rejects_column_with_initiatives()
    {
        var boardId = Guid.NewGuid();
        var column = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "C", SortOrder = "a", Type = ColumnType.InitiativeOnly };
        var initiative = new Initiative { Id = Guid.NewGuid(), BoardId = boardId, ColumnId = column.Id, Title = "I", SortOrder = "a" };

        Assert.Throws<BoardStructuralViolationException>(() =>
            StructuralDeletionGuard.EnsureColumnDeletable(column, initiativesInColumn: [initiative], subStagesInColumn: []));
    }

    // Given an initiative_task column with zero initiatives but at least one configured
    // sub-stage, When deletion of that column is attempted,
    // Then the deletion is rejected with a structural-violation error.
    [Fact]
    public void EnsureColumnDeletable_rejects_column_with_substages()
    {
        var boardId = Guid.NewGuid();
        var column = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "C", SortOrder = "a", Type = ColumnType.InitiativeTask };
        var subStage = new TaskSubStage { Id = Guid.NewGuid(), BoardId = boardId, ParentColumnId = column.Id, Name = "S1", SortOrder = "a" };

        Assert.Throws<BoardStructuralViolationException>(() =>
            StructuralDeletionGuard.EnsureColumnDeletable(column, initiativesInColumn: [], subStagesInColumn: [subStage]));
    }

    // Given a column with zero initiatives and zero sub-stages,
    // When deletion of that column is attempted,
    // Then the deletion succeeds (no exception).
    [Fact]
    public void EnsureColumnDeletable_allows_fully_empty_column()
    {
        var column = new BoardColumn { Id = Guid.NewGuid(), BoardId = Guid.NewGuid(), Name = "C", SortOrder = "a", Type = ColumnType.InitiativeOnly };

        var exception = Record.Exception(() =>
            StructuralDeletionGuard.EnsureColumnDeletable(column, initiativesInColumn: [], subStagesInColumn: []));

        Assert.Null(exception);
    }
}
