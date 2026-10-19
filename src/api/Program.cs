using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using ProjectTracker.Api.Modules.Board;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Modules.Board.Endpoints;
using ProjectTracker.Api.Modules.Board.Ordering;
using ProjectTracker.Api.Modules.Board.Persistence;
using ProjectTracker.Api.Shared;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BoardProblemDetailsExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length == 0)
        {
            policy.DisallowCredentials().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IBoardScopeAccessor, BoardScopeAccessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBoardAuthorizationService>(sp =>
{
    var environment = sp.GetRequiredService<IHostEnvironment>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var allowStub = configuration.GetValue("Board:AllowStubAuthorization", environment.IsDevelopment());
    return new StubBoardAuthorizationService(
        BoardRole.Member,
        environmentName: environment.EnvironmentName,
        allowStubAuthorization: allowStub,
        httpContextAccessor: sp.GetRequiredService<IHttpContextAccessor>());
});
builder.Services.AddScoped<BoardAuthorizationFilter>();
builder.Services.AddScoped<SaveChangesInterceptor, RowVersionInterceptor>();
builder.Services.AddSingleton<OrderingService>();

builder.Services.AddDbContext<BoardDbContext>((sp, options) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var environment = sp.GetRequiredService<IHostEnvironment>();
    var databaseConnectionString = DatabaseConnectionString.Resolve(configuration, environment);

    options.UseNpgsql(databaseConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Board");
    });

    options.AddInterceptors(sp.GetRequiredService<SaveChangesInterceptor>());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    _ = scope.ServiceProvider.GetRequiredService<IBoardAuthorizationService>();
    _ = scope.ServiceProvider.GetRequiredService<BoardDbContext>();
}

app.UseCors();
app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new { service = "ProjectTracker API", status = "running" }));
app.MapGet("/healthz/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/healthz/ready", async (BoardDbContext db, CancellationToken cancellationToken) =>
{
    var checks = new List<object>();

    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        checks.Add(new { name = "postgres", status = canConnect ? "pass" : "fail" });

        if (!canConnect)
        {
            return Results.Json(new { status = "unhealthy", checks }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        var schemaReady = !pendingMigrations.Any();
        checks.Add(new { name = "migrations", status = schemaReady ? "pass" : "fail" });

        return schemaReady
            ? Results.Ok(new { status = "ok", checks })
            : Results.Json(new { status = "unhealthy", checks }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception exception)
    {
        checks.Add(new { name = "postgres", status = "fail", error = exception.Message });
        return Results.Json(new { status = "unhealthy", checks }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapBoardEndpoints();

app.Run();

public partial class Program;
