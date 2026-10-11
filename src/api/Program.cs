using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Modules.Board.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddScoped<IBoardScopeAccessor, BoardScopeAccessor>();
builder.Services.AddScoped<IBoardAuthorizationService>(sp =>
{
    var environment = sp.GetRequiredService<IHostEnvironment>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var allowStub = configuration.GetValue("Board:AllowStubAuthorization", environment.IsDevelopment());
    return new StubBoardAuthorizationService(
        BoardRole.Member,
        environmentName: environment.EnvironmentName,
        allowStubAuthorization: allowStub);
});
builder.Services.AddScoped<SaveChangesInterceptor, RowVersionInterceptor>();

builder.Services.AddDbContext<BoardDbContext>((sp, options) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Postgres")
        ?? configuration["DATABASE_URL"]
        ?? "Host=localhost;Port=5432;Database=projecttracker;Username=postgres;Password=postgres";

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Board");
    });

    options.AddInterceptors(sp.GetRequiredService<SaveChangesInterceptor>());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    _ = scope.ServiceProvider.GetRequiredService<IBoardAuthorizationService>();
}

app.UseCors();
app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new { service = "ProjectTracker API", status = "running" }));
app.MapGet("/healthz/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/healthz/ready", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
