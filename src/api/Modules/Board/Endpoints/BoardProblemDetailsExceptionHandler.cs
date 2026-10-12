using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Endpoints;

public sealed class BoardProblemDetailsExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is BoardStructuralViolationException or BoardForbiddenException)
        {
            return WriteProblem(httpContext, StatusCodes.Status422UnprocessableEntity, "/problems/board-structure-violation", "Board structural violation");
        }

        return ValueTask.FromResult(false);
    }

    private static async ValueTask<bool> WriteProblem(HttpContext httpContext, int statusCode, string type, string title)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
        };

        await httpContext.Response.WriteAsJsonAsync(problem);
        return true;
    }
}
