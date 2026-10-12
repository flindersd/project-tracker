using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class StubBoardAuthorizationService : IBoardAuthorizationService
{
    private const string RoleHeaderName = "X-Board-Role";

    private readonly BoardRole defaultRole;
    private readonly IHttpContextAccessor? httpContextAccessor;

    public StubBoardAuthorizationService(
        BoardRole role,
        string? environmentName = "Development",
        bool allowStubAuthorization = true,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) && !allowStubAuthorization)
        {
            throw new InvalidOperationException("Stub board authorization is only allowed in Development with explicit opt-in.");
        }

        this.defaultRole = role;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task<BoardAccessContext> ResolveAsync(ClaimsPrincipal principal, Guid boardId, CancellationToken cancellationToken = default)
    {
        var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } value && Guid.TryParse(value, out var parsed)
            ? parsed
            : Guid.Empty;

        var resolvedRole = ResolveRoleFromRequestHeader() ?? defaultRole;

        return Task.FromResult(new BoardAccessContext(boardId, userId, resolvedRole));
    }

    private BoardRole? ResolveRoleFromRequestHeader()
    {
        var headerValue = httpContextAccessor?.HttpContext?.Request.Headers[RoleHeaderName].ToString();
        return !string.IsNullOrEmpty(headerValue) && Enum.TryParse<BoardRole>(headerValue, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : null;
    }
}
