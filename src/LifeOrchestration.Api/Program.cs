using Microsoft.EntityFrameworkCore;
using LifeOrchestration.Infrastructure.Data;
using LifeOrchestration.Core.Entities;
using CoreTaskStatus = LifeOrchestration.Core.Entities.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=data/lifeorchestration.db"));

// Add CORS for Blazor WASM client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Enable CORS
app.UseCors();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Map task endpoints
app.MapGet("/api/tasks", async (AppDbContext db) =>
    await db.Tasks.OrderByDescending(t => t.CreatedAt).ToListAsync());

app.MapGet("/api/tasks/{id}", async (int id, AppDbContext db) =>
    await db.Tasks.FindAsync(id) is { } task ? Results.Ok(task) : Results.NotFound());

app.MapPost("/api/tasks", async (CreateTaskRequest request, AppDbContext db) =>
{
    var task = new TaskItem
    {
        Title = request.Title,
        Assignee = request.Assignee,
        Status = CoreTaskStatus.Todo,
        CreatedAt = DateTime.UtcNow,
        DueDate = request.DueDate
    };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPatch("/api/tasks/{id}", async (int id, UpdateTaskRequest request, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    
    if (request.Status.HasValue)
        task.Status = request.Status.Value;
    if (request.DueDate.HasValue)
        task.DueDate = request.DueDate.Value;
    
    await db.SaveChangesAsync();
    return Results.Ok(task);
});

app.MapDelete("/api/tasks/{id}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    
    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public record CreateTaskRequest(string Title, string Assignee, DateTime? DueDate);
public record UpdateTaskRequest(CoreTaskStatus? Status, DateTime? DueDate);
