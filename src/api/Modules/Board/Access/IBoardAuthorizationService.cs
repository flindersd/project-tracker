using System.Security.Claims;

namespace ProjectTracker.Api.Modules.Board.Access;

public interface IBoardAuthorizationService
{
    Task<BoardAccessContext> ResolveAsync(ClaimsPrincipal principal, Guid boardId, CancellationToken cancellationToken = default);
}
