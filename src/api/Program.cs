var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new { service = "ProjectTracker API", status = "running" }));
app.MapGet("/healthz/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/healthz/ready", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
