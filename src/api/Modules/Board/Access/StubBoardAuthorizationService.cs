using System.Security.Claims;

namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class StubBoardAuthorizationService : IBoardAuthorizationService
{
    private readonly BoardRole role;

    public StubBoardAuthorizationService(
        BoardRole role,
        string? environmentName = "Development",
        bool allowStubAuthorization = true)
    {
        if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) && !allowStubAuthorization)
        {
            throw new InvalidOperationException("Stub board authorization is only allowed in Development with explicit opt-in.");
        }

        this.role = role;
    }

    public Task<BoardAccessContext> ResolveAsync(ClaimsPrincipal principal, Guid boardId, CancellationToken cancellationToken = default)
    {
        var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } value && Guid.TryParse(value, out var parsed)
            ? parsed
            : Guid.Empty;

        return Task.FromResult(new BoardAccessContext(boardId, userId, role));
    }
}
