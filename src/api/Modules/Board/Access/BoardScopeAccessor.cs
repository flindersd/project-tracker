namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class BoardScopeAccessor : IBoardScopeAccessor
{
    public Guid? CurrentBoardId { get; private set; }

    public void Set(Guid? boardId) => CurrentBoardId = boardId;
}
