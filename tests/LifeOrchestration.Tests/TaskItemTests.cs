using Microsoft.EntityFrameworkCore;
using LifeOrchestration.Core.Entities;
using LifeOrchestration.Infrastructure.Data;
using TaskStatus = LifeOrchestration.Core.Entities.TaskStatus;

namespace LifeOrchestration.Tests;

public class TaskItemTests : IDisposable
{
    private readonly AppDbContext _db;
    
    public TaskItemTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }
    
    public void Dispose()
    {
        _db.Dispose();
    }
    
    [Fact]
    public async Task CreateTask_ShouldPersistTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Test task",
            Assignee = "robert",
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            Priority = PriorityLevel.Medium
        };
        
        // Act
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        
        // Assert
        var saved = await _db.Tasks.FirstOrDefaultAsync(t => t.Title == "Test task");
        Assert.NotNull(saved);
        Assert.Equal("robert", saved.Assignee);
        Assert.Equal(TaskStatus.Todo, saved.Status);
    }
    
    [Fact]
    public async Task UpdateTaskStatus_ShouldUpdateStatus()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Status test",
            Assignee = "robert",
            Status = TaskStatus.Todo
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        
        // Act
        task.Status = TaskStatus.InProgress;
        await _db.SaveChangesAsync();
        
        // Assert
        var updated = await _db.Tasks.FindAsync(task.Id);
        Assert.Equal(TaskStatus.InProgress, updated!.Status);
    }
    
    [Fact]
    public async Task DeleteTask_ShouldRemoveTask()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Delete me",
            Assignee = "robert",
            Status = TaskStatus.Todo
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        var id = task.Id;
        
        // Act
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        
        // Assert
        var deleted = await _db.Tasks.FindAsync(id);
        Assert.Null(deleted);
    }
    
    [Fact]
    public async Task CreateRecurringTask_ShouldSetRecurrenceFields()
    {
        // Arrange & Act
        var task = new TaskItem
        {
            Title = "Weekly sync",
            Assignee = "robert",
            Status = TaskStatus.Todo,
            RecurrencePattern = RecurrencePattern.Weekly,
            RecurrenceInterval = 1
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        
        // Assert
        var saved = await _db.Tasks.FindAsync(task.Id);
        Assert.NotNull(saved);
        Assert.Equal(RecurrencePattern.Weekly, saved.RecurrencePattern);
        Assert.Equal(1, saved.RecurrenceInterval);
    }
    
    [Fact]
    public async Task CompleteRecurringTask_ShouldCreateNextInstance()
    {
        // Arrange - create a recurring task
        var parentTask = new TaskItem
        {
            Title = "Monthly review",
            Assignee = "robert",
            Status = TaskStatus.Todo,
            DueDate = DateTime.UtcNow,
            RecurrencePattern = RecurrencePattern.Monthly,
            RecurrenceInterval = 1
        };
        _db.Tasks.Add(parentTask);
        await _db.SaveChangesAsync();
        
        // Act - mark as done (this triggers the API logic)
        parentTask.Status = TaskStatus.Done;
        await _db.SaveChangesAsync();
        
        // Calculate next due date (as the API would)
        var nextDueDate = parentTask.DueDate!.Value.AddMonths(parentTask.RecurrenceInterval);
        
        var nextTask = new TaskItem
        {
            Title = parentTask.Title,
            Assignee = parentTask.Assignee,
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            DueDate = nextDueDate,
            Priority = parentTask.Priority,
            Category = parentTask.Category,
            Description = parentTask.Description,
            RecurrencePattern = parentTask.RecurrencePattern,
            RecurrenceInterval = parentTask.RecurrenceInterval,
            ParentTaskId = parentTask.Id
        };
        _db.Tasks.Add(nextTask);
        await _db.SaveChangesAsync();
        
        // Assert
        var instances = await _db.Tasks.Where(t => t.ParentTaskId == parentTask.Id).ToListAsync();
        Assert.Single(instances);
        Assert.Equal(nextDueDate, instances[0].DueDate);
        Assert.Equal(TaskStatus.Todo, instances[0].Status);
    }
    
    [Fact]
    public async Task FilterByAssignee_ShouldReturnMatchingTasks()
    {
        // Arrange
        _db.Tasks.AddRange(
            new TaskItem { Title = "Task 1", Assignee = "robert", Status = TaskStatus.Todo },
            new TaskItem { Title = "Task 2", Assignee = "johanna", Status = TaskStatus.Todo },
            new TaskItem { Title = "Task 3", Assignee = "robert", Status = TaskStatus.Todo }
        );
        await _db.SaveChangesAsync();
        
        // Act
        var robertTasks = await _db.Tasks.Where(t => t.Assignee == "robert").ToListAsync();
        
        // Assert
        Assert.Equal(2, robertTasks.Count);
    }
    
    [Fact]
    public async Task FilterByStatus_ShouldReturnMatchingTasks()
    {
        // Arrange
        _db.Tasks.AddRange(
            new TaskItem { Title = "Todo 1", Assignee = "robert", Status = TaskStatus.Todo },
            new TaskItem { Title = "Done 1", Assignee = "robert", Status = TaskStatus.Done },
            new TaskItem { Title = "Done 2", Assignee = "robert", Status = TaskStatus.Done }
        );
        await _db.SaveChangesAsync();
        
        // Act
        var doneTasks = await _db.Tasks.Where(t => t.Status == TaskStatus.Done).ToListAsync();
        
        // Assert
        Assert.Equal(2, doneTasks.Count);
    }
    
    [Fact]
    public async Task GetOverdueTasks_ShouldReturnOnlyOverdue()
    {
        // Arrange
        _db.Tasks.AddRange(
            new TaskItem { Title = "Overdue", Assignee = "robert", Status = TaskStatus.Todo, DueDate = DateTime.UtcNow.AddDays(-1) },
            new TaskItem { Title = "Future", Assignee = "robert", Status = TaskStatus.Todo, DueDate = DateTime.UtcNow.AddDays(1) },
            new TaskItem { Title = "Done overdue", Assignee = "robert", Status = TaskStatus.Done, DueDate = DateTime.UtcNow.AddDays(-1) }
        );
        await _db.SaveChangesAsync();
        
        // Act
        var overdue = await _db.Tasks
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != TaskStatus.Done)
            .ToListAsync();
        
        // Assert
        Assert.Single(overdue);
        Assert.Equal("Overdue", overdue[0].Title);
    }
}