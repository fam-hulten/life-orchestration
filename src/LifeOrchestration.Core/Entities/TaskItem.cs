namespace LifeOrchestration.Core.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
