namespace ProjectTracker.Api.Modules.Board.Domain;

public sealed class BoardStructuralViolationException : Exception
{
    public BoardStructuralViolationException(string message = "Board structural violation") : base(message)
    {
    }
}
