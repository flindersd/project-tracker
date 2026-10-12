namespace ProjectTracker.Api.Modules.Board.Access;

public interface IBoardScopeAccessor
{
    Guid? CurrentBoardId { get; }

    void Set(Guid? boardId);
}
