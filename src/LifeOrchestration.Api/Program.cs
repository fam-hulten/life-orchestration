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
app.MapGet("/api/tasks", async (
    AppDbContext db,
    string? search,
    string? assignee,
    string? category,
    CoreTaskStatus? status,
    DateTime? duebefore,
    DateTime? dueafter,
    bool? overdue) =>
{
    var query = db.Tasks.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));
    if (!string.IsNullOrWhiteSpace(assignee))
        query = query.Where(t => t.Assignee == assignee);
    if (!string.IsNullOrWhiteSpace(category))
        query = query.Where(t => t.Category == category);
    if (status.HasValue)
        query = query.Where(t => t.Status == status.Value);
    if (duebefore.HasValue)
        query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= duebefore.Value);
    if (dueafter.HasValue)
        query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= dueafter.Value);
    if (overdue == true)
        query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != CoreTaskStatus.Done);

    return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
});

app.MapGet("/api/tasks/{id}", async (int id, AppDbContext db) =>
    await db.Tasks.FindAsync(id) is { } task ? Results.Ok(task) : Results.NotFound());

app.MapPost("/api/tasks", async (CreateTaskRequest request, AppDbContext db) =>
{
    var task = new TaskItem
    {
        Title = request.Title,
        Assignee = request.Assignee,
        Requestor = request.Requestor,
        Status = CoreTaskStatus.Todo,
        CreatedAt = DateTime.UtcNow,
        DueDate = request.DueDate,
        Priority = request.Priority,
        Category = request.Category,
        Description = request.Description
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
    if (request.Requestor is not null)
        task.Requestor = request.Requestor;
    if (request.Priority.HasValue)
        task.Priority = request.Priority.Value;
    if (request.Category is not null)
        task.Category = request.Category;
    if (request.Description is not null)
        task.Description = request.Description;
    
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

public record CreateTaskRequest(string Title, string Assignee, DateTime? DueDate, string? Requestor, PriorityLevel Priority, string? Category, string? Description);
public record UpdateTaskRequest(CoreTaskStatus? Status, DateTime? DueDate, string? Requestor, PriorityLevel? Priority, string? Category, string? Description);