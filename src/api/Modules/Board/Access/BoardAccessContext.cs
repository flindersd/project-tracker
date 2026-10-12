namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class BoardAccessContext
{
    internal BoardAccessContext(Guid boardId, Guid userId, BoardRole role)
    {
        BoardId = boardId;
        UserId = userId;
        Role = role;
    }

    public Guid BoardId { get; }

    public Guid UserId { get; }

    public BoardRole Role { get; }

    public void RequireAdministrator()
    {
        if (Role != BoardRole.Administrator)
        {
            throw new BoardForbiddenException();
        }
    }
}
