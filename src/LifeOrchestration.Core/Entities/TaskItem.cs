namespace LifeOrchestration.Core.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string? Requestor { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public string? Category { get; set; }
    public string? Description { get; set; }  // Beskrivning/notes
    
    // Recurring task support
    public RecurrencePattern? RecurrencePattern { get; set; }  // Null = not recurring
    public int RecurrenceInterval { get; set; } = 1;  // Every N periods
    public int? ParentTaskId { get; set; }  // If set, this is an instance of another task
    public DateTime? NextDueDate { get; set; }  // For recurring templates: next scheduled date
}

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public enum PriorityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum RecurrencePattern
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}