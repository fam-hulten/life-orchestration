using Microsoft.EntityFrameworkCore;
using LifeOrchestration.Infrastructure.Data;
using LifeOrchestration.Core.Entities;
using CoreTaskStatus = LifeOrchestration.Core.Entities.TaskStatus;
using RecurrencePattern = LifeOrchestration.Core.Entities.RecurrencePattern;

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
    bool? overdue,
    int? parenttaskid,
    bool? recurrent) =>
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
    if (parenttaskid.HasValue)
        query = query.Where(t => t.ParentTaskId == parenttaskid.Value);
    if (recurrent == true)
        query = query.Where(t => t.RecurrencePattern.HasValue);

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
        Description = request.Description,
        RecurrencePattern = request.RecurrencePattern,
        RecurrenceInterval = request.RecurrenceInterval,
        ParentTaskId = request.ParentTaskId,
        NextDueDate = request.NextDueDate
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
    if (request.RecurrenceInterval.HasValue)
        task.RecurrenceInterval = request.RecurrenceInterval.Value;
    if (request.NextDueDate.HasValue)
        task.NextDueDate = request.NextDueDate.Value;
    
    // When a recurring task is marked Done, create the next instance
    if (request.Status.HasValue && request.Status.Value == CoreTaskStatus.Done && task.RecurrencePattern.HasValue)
    {
        var nextDueDate = CalculateNextDueDate(task.DueDate, task.RecurrencePattern.Value, task.RecurrenceInterval);
        var nextTask = new TaskItem
        {
            Title = task.Title,
            Assignee = task.Assignee,
            Requestor = task.Requestor,
            Status = CoreTaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            DueDate = nextDueDate,
            Priority = task.Priority,
            Category = task.Category,
            Description = task.Description,
            RecurrencePattern = task.RecurrencePattern,
            RecurrenceInterval = task.RecurrenceInterval,
            ParentTaskId = task.ParentTaskId ?? task.Id,
            NextDueDate = CalculateNextDueDate(nextDueDate, task.RecurrencePattern.Value, task.RecurrenceInterval)
        };
        db.Tasks.Add(nextTask);
        
        // Also update the completed task's NextDueDate
        task.NextDueDate = nextDueDate;
    }
    
    await db.SaveChangesAsync();
    return Results.Ok(task);
});

static DateTime CalculateNextDueDate(DateTime? currentDue, RecurrencePattern pattern, int interval)
{
    var baseDate = currentDue ?? DateTime.UtcNow;
    return pattern switch
    {
        RecurrencePattern.Daily => baseDate.AddDays(interval),
        RecurrencePattern.Weekly => baseDate.AddDays(7 * interval),
        RecurrencePattern.Monthly => baseDate.AddMonths(interval),
        RecurrencePattern.Yearly => baseDate.AddYears(interval),
        _ => baseDate.AddDays(interval)
    };
}

app.MapDelete("/api/tasks/{id}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    
    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public record CreateTaskRequest(string Title, string Assignee, DateTime? DueDate, string? Requestor, PriorityLevel Priority, string? Category, string? Description, RecurrencePattern? RecurrencePattern, int RecurrenceInterval, int? ParentTaskId, DateTime? NextDueDate);
public record UpdateTaskRequest(CoreTaskStatus? Status, DateTime? DueDate, string? Requestor, PriorityLevel? Priority, string? Category, string? Description, int? RecurrenceInterval, DateTime? NextDueDate);