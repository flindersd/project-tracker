using ProjectTracker.Api.Modules.Board.Domain;

namespace ProjectTracker.Api.Modules.Board.Endpoints.Dtos;

public sealed record BoardResponse(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CreateBoardRequest(string Name);
public sealed record UpdateBoardRequest(string Name);

public sealed record BoardColumnResponse(Guid Id, Guid BoardId, string Name, string SortOrder, ColumnType ColumnType, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CreateBoardColumnRequest(string Name, ColumnType ColumnType, Guid? AfterId, Guid? BeforeId);
public sealed record UpdateBoardColumnRequest(string Name, ColumnType ColumnType);

public sealed record TaskSubStageResponse(Guid Id, Guid BoardId, Guid ParentColumnId, string Name, string SortOrder, bool IsInactive);
public sealed record CreateTaskSubStageRequest(string Name, bool IsInactive, Guid? AfterId, Guid? BeforeId);
public sealed record UpdateTaskSubStageRequest(string Name, bool IsInactive);

public sealed record InitiativeResponse(Guid Id, Guid BoardId, Guid ColumnId, string Title, string? Description, string SortOrder);
public sealed record CreateInitiativeRequest(Guid ColumnId, string Title, string? Description, Guid? AfterId, Guid? BeforeId);
public sealed record UpdateInitiativeRequest(string Title, string? Description);
public sealed record MoveInitiativeRequest(Guid ColumnId, Guid? AfterId, Guid? BeforeId);

public sealed record TaskResponse(Guid Id, Guid BoardId, Guid InitiativeId, Guid? TaskSubStageId, Guid? DormantSubStageId, string Title, string? Description, DateOnly? DueDate, TaskPriority Priority, string SortOrder, long RowVersion);
public sealed record CreateTaskRequest(Guid InitiativeId, string Title, string? Description, DateOnly? DueDate, TaskPriority Priority, Guid? TaskSubStageId, Guid? AfterId, Guid? BeforeId);
public sealed record UpdateTaskRequest(Guid InitiativeId, string Title, string? Description, DateOnly? DueDate, TaskPriority Priority, Guid? TaskSubStageId);
public sealed record MoveTaskRequest(Guid? InitiativeId, Guid? TaskSubStageId, Guid? AfterId, Guid? BeforeId);

public sealed record TagResponse(Guid Id, Guid BoardId, string Name, string? Color);
public sealed record CreateTagRequest(string Name, string? Color);
public sealed record UpdateTagRequest(string Name, string? Color);
