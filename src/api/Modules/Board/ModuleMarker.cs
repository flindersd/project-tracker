using Microsoft.AspNetCore.Routing;
using ProjectTracker.Api.Modules.Board.Endpoints;

namespace ProjectTracker.Api.Modules.Board;

public static class ModuleMarker
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app) => BoardEndpointMap.Map(app);
}
