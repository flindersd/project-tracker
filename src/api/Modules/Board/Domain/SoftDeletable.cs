namespace ProjectTracker.Api.Modules.Board.Domain;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}
