using Microsoft.AspNetCore.Http.HttpResults;

namespace ProjectTracker.Api.Modules.Board.Access;

public sealed class BoardAuthorizationFilter : IEndpointFilter
{
    public const string HttpContextItemKey = "BoardAccessContext";

    private readonly IBoardAuthorizationService authorizationService;
    private readonly IBoardScopeAccessor boardScopeAccessor;

    public BoardAuthorizationFilter(IBoardAuthorizationService authorizationService, IBoardScopeAccessor boardScopeAccessor)
    {
        this.authorizationService = authorizationService;
        this.boardScopeAccessor = boardScopeAccessor;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!TryGetBoardId(context.HttpContext, out var boardId))
        {
            return TypedResults.NotFound();
        }

        BoardAccessContext accessContext;
        try
        {
            accessContext = await authorizationService.ResolveAsync(context.HttpContext.User, boardId, context.HttpContext.RequestAborted);
        }
        catch
        {
            return TypedResults.NotFound();
        }

        boardScopeAccessor.Set(accessContext.BoardId);
        context.HttpContext.Items[HttpContextItemKey] = accessContext;

        return await next(context);
    }

    private static bool TryGetBoardId(HttpContext httpContext, out Guid boardId)
    {
        boardId = default;

        if (!httpContext.Request.RouteValues.TryGetValue("boardId", out var rawBoardId))
        {
            return false;
        }

        return Guid.TryParse(rawBoardId?.ToString(), out boardId);
    }
}
