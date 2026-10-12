namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class BoardForbiddenException : Exception
{
    public BoardForbiddenException(string message = "Board access forbidden") : base(message)
    {
    }
}
