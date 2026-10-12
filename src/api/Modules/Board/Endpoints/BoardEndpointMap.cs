using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Modules.Board.Domain;
using ProjectTracker.Api.Modules.Board.Endpoints.Dtos;
using ProjectTracker.Api.Modules.Board.Ordering;
using ProjectTracker.Api.Modules.Board.Persistence;
using ProjectTracker.Api.Modules.Board.Services;
using ProjectTracker.Api.Shared;

namespace ProjectTracker.Api.Modules.Board.Endpoints;

public static class BoardEndpointMap
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var boards = app.MapGroup("/api/boards");

        boards.MapGet(string.Empty, ListBoardsAsync);
        boards.MapPost(string.Empty, CreateBoardAsync);

        var boardScoped = boards.MapGroup("/{boardId:guid}");
        boardScoped.AddEndpointFilter<BoardAuthorizationFilter>();

        boardScoped.MapGet(string.Empty, GetBoardAsync);
        boardScoped.MapPut(string.Empty, UpdateBoardAsync);
        boardScoped.MapDelete(string.Empty, DeleteBoardAsync);

        var columns = boardScoped.MapGroup("/columns");
        columns.MapGet(string.Empty, ListColumnsAsync);
        columns.MapPost(string.Empty, CreateColumnAsync).RequireAuthorization();
        columns.MapGet("/{columnId:guid}", GetColumnAsync);
        columns.MapPut("/{columnId:guid}", UpdateColumnAsync).RequireAuthorization();
        columns.MapDelete("/{columnId:guid}", DeleteColumnAsync).RequireAuthorization();

        var subStages = boardScoped.MapGroup("/columns/{columnId:guid}/sub-stages");
        subStages.MapGet(string.Empty, ListSubStagesAsync);
        subStages.MapPost(string.Empty, CreateSubStageAsync).RequireAuthorization();
        subStages.MapGet("/{subStageId:guid}", GetSubStageAsync);
        subStages.MapPut("/{subStageId:guid}", UpdateSubStageAsync).RequireAuthorization();
        subStages.MapDelete("/{subStageId:guid}", DeleteSubStageAsync).RequireAuthorization();

        var initiatives = boardScoped.MapGroup("/initiatives");
        initiatives.MapGet(string.Empty, ListInitiativesAsync);
        initiatives.MapPost(string.Empty, CreateInitiativeAsync);
        initiatives.MapGet("/{initiativeId:guid}", GetInitiativeAsync);
        initiatives.MapPut("/{initiativeId:guid}", UpdateInitiativeAsync);
        initiatives.MapDelete("/{initiativeId:guid}", DeleteInitiativeAsync).RequireAuthorization();
        initiatives.MapPost("/{initiativeId:guid}:move", MoveInitiativeAsync);

        var tasks = boardScoped.MapGroup("/tasks");
        tasks.MapGet(string.Empty, ListTasksAsync);
        tasks.MapPost(string.Empty, CreateTaskAsync);
        tasks.MapGet("/{taskId:guid}", GetTaskAsync);
        tasks.MapPut("/{taskId:guid}", UpdateTaskAsync);
        tasks.MapDelete("/{taskId:guid}", DeleteTaskAsync);
        tasks.MapPost("/{taskId:guid}:move", MoveTaskAsync);
        tasks.MapPost("/{taskId:guid}/assignees", AddAssigneeAsync);
        tasks.MapDelete("/{taskId:guid}/assignees/{userId:guid}", RemoveAssigneeAsync);
        tasks.MapPost("/{taskId:guid}/tags", AddTagAsync);
        tasks.MapDelete("/{taskId:guid}/tags/{tagId:guid}", RemoveTagAsync);

        var tags = boardScoped.MapGroup("/tags");
        tags.MapGet(string.Empty, ListTagsAsync);
        tags.MapPost(string.Empty, CreateTagAsync);
        tags.MapGet("/{tagId:guid}", GetTagAsync);
        tags.MapPut("/{tagId:guid}", UpdateTagAsync);
        tags.MapDelete("/{tagId:guid}", DeleteTagAsync);

        return app;
    }

    private static async Task<Ok<IEnumerable<BoardResponse>>> ListBoardsAsync(BoardDbContext db, CancellationToken cancellationToken)
    {
        var boards = await db.Boards.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BoardResponse(x.Id, x.Name, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<BoardResponse>>(boards);
    }

    private static async Task<Created<BoardResponse>> CreateBoardAsync(CreateBoardRequest request, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        var board = new global::ProjectTracker.Api.Modules.Board.Domain.Board
        {
            Id = UuidV7.NewGuid(),
            Name = request.Name,
            CreatedBy = GetCurrentUserId(httpContext),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Boards.Add(board);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/boards/{board.Id}", new BoardResponse(board.Id, board.Name, board.CreatedAt, board.UpdatedAt));
    }

    private static async Task<Results<Ok<BoardResponse>, NotFound>> GetBoardAsync(Guid boardId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var board = await db.Boards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == boardId, cancellationToken);
        return board is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new BoardResponse(board.Id, board.Name, board.CreatedAt, board.UpdatedAt));
    }

    private static async Task<Results<Ok<BoardResponse>, NotFound, UnauthorizedHttpResult>> UpdateBoardAsync(Guid boardId, UpdateBoardRequest request, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        if (!TryGetAccessContext(httpContext, out var accessContext))
        {
            return TypedResults.Unauthorized();
        }

        accessContext.RequireAdministrator();

        var board = await db.Boards.FirstOrDefaultAsync(x => x.Id == boardId, cancellationToken);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        board.Name = request.Name;
        board.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new BoardResponse(board.Id, board.Name, board.CreatedAt, board.UpdatedAt));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteBoardAsync(Guid boardId, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        if (!TryGetAccessContext(httpContext, out var accessContext))
        {
            return TypedResults.Unauthorized();
        }

        accessContext.RequireAdministrator();

        var board = await db.Boards.FirstOrDefaultAsync(x => x.Id == boardId, cancellationToken);
        if (board is null)
        {
            return TypedResults.NotFound();
        }

        board.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IEnumerable<BoardColumnResponse>>> ListColumnsAsync(Guid boardId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var columns = await db.BoardColumns.AsNoTracking()
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.SortOrder, StringComparer.Ordinal)
            .Select(x => new BoardColumnResponse(x.Id, x.BoardId, x.Name, x.SortOrder, x.Type, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<BoardColumnResponse>>(columns);
    }

    private static async Task<Results<Created<BoardColumnResponse>, UnauthorizedHttpResult>> CreateColumnAsync(Guid boardId, CreateBoardColumnRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var sortOrder = await ComputeSortOrderAsync(
            db.BoardColumns.AsNoTracking().Where(x => x.BoardId == boardId),
            request.AfterId,
            request.BeforeId,
            column => column.Id,
            column => column.SortOrder,
            orderingService,
            cancellationToken);

        var column = new BoardColumn
        {
            Id = UuidV7.NewGuid(),
            BoardId = boardId,
            Name = request.Name,
            Type = request.ColumnType,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.BoardColumns.Add(column);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/boards/{boardId}/columns/{column.Id}", ToColumnResponse(column));
    }

    private static async Task<Results<Ok<BoardColumnResponse>, NotFound>> GetColumnAsync(Guid boardId, Guid columnId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var column = await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == columnId, cancellationToken);
        return column is null ? TypedResults.NotFound() : TypedResults.Ok(ToColumnResponse(column));
    }

    private static async Task<Results<Ok<BoardColumnResponse>, NotFound, UnauthorizedHttpResult>> UpdateColumnAsync(Guid boardId, Guid columnId, UpdateBoardColumnRequest request, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == columnId, cancellationToken);
        if (column is null)
        {
            return TypedResults.NotFound();
        }

        if (column.Type != request.ColumnType)
        {
            var subStages = await db.TaskSubStages.Where(x => x.BoardId == boardId && x.ParentColumnId == columnId).ToListAsync(cancellationToken);
            var initiativeIds = await db.Initiatives
                .Where(x => x.BoardId == boardId && x.ColumnId == columnId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var tasks = await db.Tasks.Where(x => x.BoardId == boardId && initiativeIds.Contains(x.InitiativeId)).ToListAsync(cancellationToken);

            if (request.ColumnType == ColumnType.InitiativeOnly)
            {
                var result = ColumnTypeChangeService.ChangeToInitiativeOnly(column, subStages, tasks);
                ApplyColumnChange(column, result.Column);
                ApplyTaskChanges(tasks, result.Tasks);
                db.TaskSubStages.RemoveRange(subStages);
            }
            else
            {
                var result = ColumnTypeChangeService.ChangeToInitiativeTask(column, tasks);
                ApplyColumnChange(column, result.Column);
                ApplyTaskChanges(tasks, result.Tasks);
            }
        }

        column.Name = request.Name;
        column.Type = request.ColumnType;
        column.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToColumnResponse(column));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteColumnAsync(Guid boardId, Guid columnId, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == columnId, cancellationToken);
        if (column is null)
        {
            return TypedResults.NotFound();
        }

        var initiatives = await db.Initiatives.Where(x => x.BoardId == boardId && x.ColumnId == columnId).ToListAsync(cancellationToken);
        var subStages = await db.TaskSubStages.Where(x => x.BoardId == boardId && x.ParentColumnId == columnId).ToListAsync(cancellationToken);
        StructuralDeletionGuard.EnsureColumnDeletable(column, initiatives, subStages);

        db.BoardColumns.Remove(column);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IEnumerable<TaskSubStageResponse>>> ListSubStagesAsync(Guid boardId, Guid columnId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var subStages = await db.TaskSubStages.AsNoTracking()
            .Where(x => x.BoardId == boardId && x.ParentColumnId == columnId)
            .OrderBy(x => x.SortOrder, StringComparer.Ordinal)
            .Select(x => new TaskSubStageResponse(x.Id, x.BoardId, x.ParentColumnId, x.Name, x.SortOrder, x.IsInactive))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<TaskSubStageResponse>>(subStages);
    }

    private static async Task<Results<Created<TaskSubStageResponse>, NotFound, UnauthorizedHttpResult>> CreateSubStageAsync(Guid boardId, Guid columnId, CreateTaskSubStageRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var column = await db.BoardColumns.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == columnId, cancellationToken);
        if (column is null)
        {
            return TypedResults.NotFound();
        }

        var sortOrder = await ComputeSortOrderAsync(
            db.TaskSubStages.AsNoTracking().Where(x => x.BoardId == boardId && x.ParentColumnId == columnId),
            request.AfterId,
            request.BeforeId,
            subStage => subStage.Id,
            subStage => subStage.SortOrder,
            orderingService,
            cancellationToken);

        var subStage = TaskSubStage.Create(column, request.Name, sortOrder);
        if (request.IsInactive)
        {
            TaskSubStage.MarkInactive(subStage);
        }

        db.TaskSubStages.Add(subStage);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/boards/{boardId}/columns/{columnId}/sub-stages/{subStage.Id}", ToSubStageResponse(subStage));
    }

    private static async Task<Results<Ok<TaskSubStageResponse>, NotFound>> GetSubStageAsync(Guid boardId, Guid columnId, Guid subStageId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var subStage = await db.TaskSubStages.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.ParentColumnId == columnId && x.Id == subStageId, cancellationToken);
        return subStage is null ? TypedResults.NotFound() : TypedResults.Ok(ToSubStageResponse(subStage));
    }

    private static async Task<Results<Ok<TaskSubStageResponse>, NotFound, UnauthorizedHttpResult>> UpdateSubStageAsync(Guid boardId, Guid columnId, Guid subStageId, UpdateTaskSubStageRequest request, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var subStage = await db.TaskSubStages.FirstOrDefaultAsync(x => x.BoardId == boardId && x.ParentColumnId == columnId && x.Id == subStageId, cancellationToken);
        if (subStage is null)
        {
            return TypedResults.NotFound();
        }

        var tasks = await db.Tasks.Where(x => x.BoardId == boardId && x.TaskSubStageId == subStageId).ToListAsync(cancellationToken);
        var initiatives = await db.Initiatives.Where(x => x.BoardId == boardId).ToListAsync(cancellationToken);

        subStage.Name = request.Name;
        if (request.IsInactive)
        {
            TaskSubStage.SetInactive(subStage);
        }
        else
        {
            TaskSubStage.ClearInactive(subStage, initiatives, tasks);
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToSubStageResponse(subStage));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteSubStageAsync(Guid boardId, Guid columnId, Guid subStageId, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var subStage = await db.TaskSubStages.FirstOrDefaultAsync(x => x.BoardId == boardId && x.ParentColumnId == columnId && x.Id == subStageId, cancellationToken);
        if (subStage is null)
        {
            return TypedResults.NotFound();
        }

        var tasks = await db.Tasks.Where(x => x.BoardId == boardId && x.TaskSubStageId == subStageId).ToListAsync(cancellationToken);
        StructuralDeletionGuard.EnsureSubStageDeletable(subStage, tasks);

        db.TaskSubStages.Remove(subStage);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IEnumerable<InitiativeResponse>>> ListInitiativesAsync(Guid boardId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var initiatives = await db.Initiatives.AsNoTracking()
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.SortOrder, StringComparer.Ordinal)
            .Select(x => new InitiativeResponse(x.Id, x.BoardId, x.ColumnId, x.Title, x.Description, x.SortOrder))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<InitiativeResponse>>(initiatives);
    }

    private static async Task<Results<Created<InitiativeResponse>, NotFound, UnauthorizedHttpResult>> CreateInitiativeAsync(Guid boardId, CreateInitiativeRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var column = await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.ColumnId, cancellationToken);
        if (column is null)
        {
            return TypedResults.NotFound();
        }

        var sortOrder = await ComputeSortOrderAsync(
            db.Initiatives.AsNoTracking().Where(x => x.BoardId == boardId && x.ColumnId == request.ColumnId),
            request.AfterId,
            request.BeforeId,
            initiative => initiative.Id,
            initiative => initiative.SortOrder,
            orderingService,
            cancellationToken);

        var initiative = new Initiative
        {
            Id = UuidV7.NewGuid(),
            BoardId = boardId,
            ColumnId = request.ColumnId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Initiatives.Add(initiative);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/boards/{boardId}/initiatives/{initiative.Id}", ToInitiativeResponse(initiative));
    }

    private static async Task<Results<Ok<InitiativeResponse>, NotFound>> GetInitiativeAsync(Guid boardId, Guid initiativeId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var initiative = await db.Initiatives.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiativeId, cancellationToken);
        return initiative is null ? TypedResults.NotFound() : TypedResults.Ok(ToInitiativeResponse(initiative));
    }

    private static async Task<Results<Ok<InitiativeResponse>, NotFound, UnauthorizedHttpResult>> UpdateInitiativeAsync(Guid boardId, Guid initiativeId, UpdateInitiativeRequest request, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var initiative = await db.Initiatives.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiativeId, cancellationToken);
        if (initiative is null)
        {
            return TypedResults.NotFound();
        }

        initiative.Title = request.Title;
        initiative.Description = request.Description;

        initiative.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToInitiativeResponse(initiative));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteInitiativeAsync(Guid boardId, Guid initiativeId, HttpContext httpContext, BoardDbContext db, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var initiative = await db.Initiatives.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiativeId, cancellationToken);
        if (initiative is null)
        {
            return TypedResults.NotFound();
        }

        initiative.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<InitiativeResponse>, NotFound, UnauthorizedHttpResult>> MoveInitiativeAsync(Guid boardId, Guid initiativeId, MoveInitiativeRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        RequireAdministrator(httpContext);

        var initiative = await db.Initiatives.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiativeId, cancellationToken);
        if (initiative is null)
        {
            return TypedResults.NotFound();
        }

        var destinationColumn = await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.ColumnId, cancellationToken);
        if (destinationColumn is null)
        {
            return TypedResults.NotFound();
        }

        var tasks = await db.Tasks.Where(x => x.BoardId == boardId && x.InitiativeId == initiativeId).ToListAsync(cancellationToken);
        var subStages = await db.TaskSubStages.AsNoTracking().Where(x => x.BoardId == boardId && x.ParentColumnId == destinationColumn.Id).ToListAsync(cancellationToken);
        var lookup = await LoadSubStageLookupAsync(db, boardId, tasks, cancellationToken);

        var result = InitiativeMoveService.Move(initiative, tasks, destinationColumn, subStages, id => id is null ? null : lookup.TryGetValue(id.Value, out var subStage) ? subStage : null);
        ApplyInitiativeMove(initiative, result.Initiative);
        ApplyTaskChanges(tasks, result.Tasks);

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToInitiativeResponse(initiative));
    }

    private static async Task<Ok<IEnumerable<TaskResponse>>> ListTasksAsync(Guid boardId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tasks = await db.Tasks.AsNoTracking()
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.SortOrder, StringComparer.Ordinal)
            .Select(x => new TaskResponse(x.Id, x.BoardId, x.InitiativeId, x.TaskSubStageId, x.DormantSubStageId, x.Title, x.Description, x.DueDate, x.Priority, x.SortOrder, x.RowVersion))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<TaskResponse>>(tasks);
    }

    private static async Task<Results<Created<TaskResponse>, NotFound>> CreateTaskAsync(Guid boardId, CreateTaskRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        var initiative = await db.Initiatives.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.InitiativeId, cancellationToken);
        if (initiative is null)
        {
            return TypedResults.NotFound();
        }

        var column = await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiative.ColumnId, cancellationToken);
        if (column is null)
        {
            return TypedResults.NotFound();
        }

        TaskSubStage? taskSubStage = null;
        if (request.TaskSubStageId is not null)
        {
            taskSubStage = await db.TaskSubStages.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.TaskSubStageId.Value, cancellationToken);
            if (taskSubStage is null || taskSubStage.ParentColumnId != initiative.ColumnId || taskSubStage.IsInactive)
            {
                throw new BoardStructuralViolationException();
            }
        }
        else if (column.Type == ColumnType.InitiativeTask)
        {
            taskSubStage = await db.TaskSubStages.AsNoTracking()
                .Where(x => x.BoardId == boardId && x.ParentColumnId == initiative.ColumnId)
                .OrderBy(x => x.SortOrder, StringComparer.Ordinal)
                .FirstOrDefaultAsync(cancellationToken);

            if (taskSubStage is null)
            {
                throw new BoardStructuralViolationException();
            }
        }

        var resolvedTaskSubStageId = taskSubStage?.Id;

        var sortOrder = await ComputeSortOrderAsync(
            db.Tasks.AsNoTracking().Where(x => x.BoardId == boardId && x.InitiativeId == request.InitiativeId && x.TaskSubStageId == resolvedTaskSubStageId),
            request.AfterId,
            request.BeforeId,
            task => task.Id,
            task => task.SortOrder,
            orderingService,
            cancellationToken);

        var task = new TaskItem
        {
            Id = UuidV7.NewGuid(),
            BoardId = boardId,
            CreatedBy = GetCurrentUserId(httpContext),
            InitiativeId = request.InitiativeId,
            TaskSubStageId = taskSubStage?.Id,
            DormantSubStageId = null,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Priority = request.Priority,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/boards/{boardId}/tasks/{task.Id}", ToTaskResponse(task));
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> GetTaskAsync(Guid boardId, Guid taskId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        return task is null ? TypedResults.NotFound() : TypedResults.Ok(ToTaskResponse(task));
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request, BoardDbContext db, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return TypedResults.NotFound();
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToTaskResponse(task));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTaskAsync(Guid boardId, Guid taskId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return TypedResults.NotFound();
        }

        task.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound, UnauthorizedHttpResult>> MoveTaskAsync(Guid boardId, Guid taskId, MoveTaskRequest request, HttpContext httpContext, BoardDbContext db, OrderingService orderingService, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return TypedResults.NotFound();
        }

        var initiative = await db.Initiatives.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == task.InitiativeId, cancellationToken);
        if (initiative is null)
        {
            return TypedResults.NotFound();
        }

        var sourceColumn = await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == initiative.ColumnId, cancellationToken);
        if (sourceColumn is null)
        {
            return TypedResults.NotFound();
        }

        var destinationInitiative = initiative;
        if (request.InitiativeId is not null && request.InitiativeId != task.InitiativeId)
        {
            destinationInitiative = await db.Initiatives.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.InitiativeId.Value, cancellationToken);
            if (destinationInitiative is null)
            {
                return TypedResults.NotFound();
            }
        }

        var destinationColumn = destinationInitiative.ColumnId == initiative.ColumnId
            ? sourceColumn
            : await db.BoardColumns.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == destinationInitiative.ColumnId, cancellationToken);
        if (destinationColumn is null)
        {
            return TypedResults.NotFound();
        }

        TaskSubStage? destinationSubStage = null;
        if (request.TaskSubStageId is not null)
        {
            destinationSubStage = await db.TaskSubStages.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.TaskSubStageId.Value, cancellationToken);
            if (destinationSubStage is null)
            {
                return TypedResults.NotFound();
            }

            if (destinationSubStage.ParentColumnId != destinationColumn.Id)
            {
                throw new BoardStructuralViolationException();
            }
        }

        var destinationSubStages = await db.TaskSubStages.AsNoTracking().Where(x => x.BoardId == boardId && x.ParentColumnId == destinationColumn.Id).ToListAsync(cancellationToken);

        if (request.TaskSubStageId is not null)
        {
            task.TaskSubStageId = destinationSubStage?.Id;
        }
        else if (destinationInitiative.Id != initiative.Id)
        {
            if (destinationColumn.Type == ColumnType.InitiativeOnly)
            {
                task.DormantSubStageId ??= task.TaskSubStageId;
                task.TaskSubStageId = null;
            }
            else if (destinationColumn.Id != sourceColumn.Id)
            {
                var firstDestinationSubStage = destinationSubStages.OrderBy(x => x.SortOrder, StringComparer.Ordinal).FirstOrDefault();
                if (firstDestinationSubStage is null)
                {
                    throw new BoardStructuralViolationException();
                }

                task.TaskSubStageId = firstDestinationSubStage.Id;
            }
        }

        task.InitiativeId = destinationInitiative.Id;

        var sortOrder = await ComputeSortOrderAsync(
            db.Tasks.AsNoTracking().Where(x => x.BoardId == boardId && x.InitiativeId == destinationInitiative.Id && x.TaskSubStageId == task.TaskSubStageId),
            request.AfterId,
            request.BeforeId,
            item => item.Id,
            item => item.SortOrder,
            orderingService,
            cancellationToken);

        task.SortOrder = sortOrder;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToTaskResponse(task));
    }

    private static async Task<Ok<IEnumerable<TagResponse>>> ListTagsAsync(Guid boardId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tags = await db.Tags.AsNoTracking()
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.Name)
            .Select(x => new TagResponse(x.Id, x.BoardId, x.Name, x.Color))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<TagResponse>>(tags);
    }

    private static async Task<Created<TagResponse>> CreateTagAsync(Guid boardId, CreateTagRequest request, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tag = new Tag
        {
            Id = UuidV7.NewGuid(),
            BoardId = boardId,
            Name = request.Name,
            Color = request.Color,
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/api/boards/{boardId}/tags/{tag.Id}", new TagResponse(tag.Id, tag.BoardId, tag.Name, tag.Color));
    }

    private static async Task<Results<Ok<TagResponse>, NotFound>> GetTagAsync(Guid boardId, Guid tagId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == tagId, cancellationToken);
        return tag is null ? TypedResults.NotFound() : TypedResults.Ok(new TagResponse(tag.Id, tag.BoardId, tag.Name, tag.Color));
    }

    private static async Task<Results<Ok<TagResponse>, NotFound>> UpdateTagAsync(Guid boardId, Guid tagId, UpdateTagRequest request, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        tag.Name = request.Name;
        tag.Color = request.Color;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new TagResponse(tag.Id, tag.BoardId, tag.Name, tag.Color));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTagAsync(Guid boardId, Guid tagId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        tag.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> AddAssigneeAsync(Guid boardId, Guid taskId, AddAssigneeRequest request, BoardDbContext db, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return TypedResults.NotFound();
        }

        db.TaskAssignees.Add(new TaskAssignee
        {
            BoardId = boardId,
            TaskId = taskId,
            UserId = request.UserId,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = request.AssignedBy,
        });

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveAssigneeAsync(Guid boardId, Guid taskId, Guid userId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var assignee = await db.TaskAssignees.FirstOrDefaultAsync(x => x.BoardId == boardId && x.TaskId == taskId && x.UserId == userId, cancellationToken);
        if (assignee is null)
        {
            return TypedResults.NotFound();
        }

        db.TaskAssignees.Remove(assignee);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> AddTagAsync(Guid boardId, Guid taskId, AddTaskTagRequest request, BoardDbContext db, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == taskId, cancellationToken);
        var tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.BoardId == boardId && x.Id == request.TagId, cancellationToken);
        if (task is null || tag is null)
        {
            return TypedResults.NotFound();
        }

        db.TaskTags.Add(new TaskTag { BoardId = boardId, TaskId = taskId, TagId = request.TagId });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveTagAsync(Guid boardId, Guid taskId, Guid tagId, BoardDbContext db, CancellationToken cancellationToken)
    {
        var join = await db.TaskTags.FirstOrDefaultAsync(x => x.BoardId == boardId && x.TaskId == taskId && x.TagId == tagId, cancellationToken);
        if (join is null)
        {
            return TypedResults.NotFound();
        }

        db.TaskTags.Remove(join);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static bool TryGetAccessContext(HttpContext httpContext, out BoardAccessContext accessContext)
    {
        accessContext = httpContext.Items.TryGetValue(BoardAuthorizationFilter.HttpContextItemKey, out var value) && value is BoardAccessContext ctx
            ? ctx
            : null!;
        return accessContext is not null;
    }

    private static void RequireAdministrator(HttpContext httpContext)
    {
        if (!TryGetAccessContext(httpContext, out var accessContext))
        {
            throw new BoardForbiddenException();
        }

        accessContext.RequireAdministrator();
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var raw = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
    }

    private static BoardColumnResponse ToColumnResponse(BoardColumn column)
        => new(column.Id, column.BoardId, column.Name, column.SortOrder, column.Type, column.CreatedAt, column.UpdatedAt);

    private static TaskSubStageResponse ToSubStageResponse(TaskSubStage subStage)
        => new(subStage.Id, subStage.BoardId, subStage.ParentColumnId, subStage.Name, subStage.SortOrder, subStage.IsInactive);

    private static InitiativeResponse ToInitiativeResponse(Initiative initiative)
        => new(initiative.Id, initiative.BoardId, initiative.ColumnId, initiative.Title, initiative.Description, initiative.SortOrder);

    private static TaskResponse ToTaskResponse(TaskItem task)
        => new(task.Id, task.BoardId, task.InitiativeId, task.TaskSubStageId, task.DormantSubStageId, task.Title, task.Description, task.DueDate, task.Priority, task.SortOrder, task.RowVersion);

    private static void ApplyColumnChange(BoardColumn target, BoardColumn source)
    {
        target.Name = source.Name;
        target.Type = source.Type;
        target.SortOrder = source.SortOrder;
    }

    private static void ApplyInitiativeMove(Initiative target, Initiative source)
    {
        target.ColumnId = source.ColumnId;
        target.SortOrder = source.SortOrder;
    }

    private static void ApplyTaskChanges(IReadOnlyCollection<TaskItem> targets, IReadOnlyList<TaskItem> source)
    {
        var byId = targets.ToDictionary(x => x.Id);
        foreach (var task in source)
        {
            if (!byId.TryGetValue(task.Id, out var target))
            {
                continue;
            }

            target.InitiativeId = task.InitiativeId;
            target.TaskSubStageId = task.TaskSubStageId;
            target.DormantSubStageId = task.DormantSubStageId;
            target.SortOrder = task.SortOrder;
        }
    }

    private static async Task<Dictionary<Guid, TaskSubStage>> LoadSubStageLookupAsync(BoardDbContext db, Guid boardId, IReadOnlyCollection<TaskItem> tasks, CancellationToken cancellationToken)
    {
        var ids = tasks.Select(x => x.TaskSubStageId).Where(x => x is not null).Select(x => x!.Value).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, TaskSubStage>();
        }

        var subStages = await db.TaskSubStages.AsNoTracking()
            .Where(x => x.BoardId == boardId && ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return subStages.ToDictionary(x => x.Id);
    }

    private static async Task<string> ComputeSortOrderAsync<TEntity>(IQueryable<TEntity> scope, Guid? afterId, Guid? beforeId, Func<TEntity, Guid> idSelector, Func<TEntity, string> sortOrderSelector, OrderingService orderingService, CancellationToken cancellationToken)
        where TEntity : class
    {
        var items = await scope.AsNoTracking().ToListAsync(cancellationToken);
        var after = afterId is null ? null : items.FirstOrDefault(x => idSelector(x) == afterId.Value);
        var before = beforeId is null ? null : items.FirstOrDefault(x => idSelector(x) == beforeId.Value);
        var result = orderingService.InsertBetween(after is null ? null : sortOrderSelector(after), before is null ? null : sortOrderSelector(before));

        if (result.NeedsRebalance)
        {
            var rebalance = orderingService.RebalanceScope(items.Count + 1);
            return rebalance.Last();
        }

        return result.SortOrder;
    }
}

public sealed record AddAssigneeRequest(Guid UserId, Guid AssignedBy);
public sealed record AddTaskTagRequest(Guid TagId);
