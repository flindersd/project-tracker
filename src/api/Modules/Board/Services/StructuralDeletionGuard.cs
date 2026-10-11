using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Services;

public static class StructuralDeletionGuard
{
    public static void EnsureSubStageDeletable(TaskSubStage subStage, IReadOnlyCollection<TaskItem> tasksReferencingSubStage)
    {
        if (tasksReferencingSubStage.Count > 0)
        {
            throw new BoardStructuralViolationException();
        }
    }

    public static void EnsureColumnDeletable(BoardColumn column, IReadOnlyCollection<Initiative> initiativesInColumn, IReadOnlyCollection<TaskSubStage> subStagesInColumn)
    {
        if (initiativesInColumn.Count > 0 || subStagesInColumn.Count > 0)
        {
            throw new BoardStructuralViolationException();
        }
    }
}
