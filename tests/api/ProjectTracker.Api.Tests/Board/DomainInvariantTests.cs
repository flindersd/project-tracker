using ProjectTracker.Api.Modules.Board.Domain;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class DomainInvariantTests
{
    // Given a column of type initiative_only,
    // When a TaskSubStage is created with that column as its parent,
    // Then creation is rejected (FR-03: sub-stages cannot exist under a non-initiative_task
    // column).
    [Fact]
    public void TaskSubStage_create_rejects_parent_column_that_is_not_initiative_task()
    {
        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = Guid.NewGuid(),
            Name = "Column",
            SortOrder = "a",
            Type = ColumnType.InitiativeOnly,
        };

        Assert.Throws<BoardStructuralViolationException>(() =>
            TaskSubStage.Create(column, name: "Sub-stage", sortOrder: "a"));
    }

    // Given a column of type initiative_task,
    // When a TaskSubStage is created with that column as its parent,
    // Then creation succeeds.
    [Fact]
    public void TaskSubStage_create_succeeds_for_initiative_task_column()
    {
        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = Guid.NewGuid(),
            Name = "Column",
            SortOrder = "a",
            Type = ColumnType.InitiativeTask,
        };

        var subStage = TaskSubStage.Create(column, name: "Sub-stage", sortOrder: "a");

        Assert.Equal(column.Id, subStage.ParentColumnId);
        Assert.False(subStage.IsInactive);
    }

    // Given the TaskPriority enum,
    // When its defined members are enumerated,
    // Then they are exactly Low, Medium, High, Urgent — no more, no fewer (FR-06).
    [Fact]
    public void TaskPriority_enum_has_exactly_the_four_fixed_values()
    {
        var names = Enum.GetNames<TaskPriority>();

        Assert.Equal(
            new[] { "Low", "Medium", "High", "Urgent" },
            names,
            StringComparer.Ordinal);
    }
}
