using System.Net.Http.Json;
using CoreTaskStatus = LifeOrchestration.Core.Entities.TaskStatus;
using PriorityLevel = LifeOrchestration.Core.Entities.PriorityLevel;
using RecurrencePattern = LifeOrchestration.Core.Entities.RecurrencePattern;

var baseUrl = Environment.GetEnvironmentVariable("PM_API_URL") ?? "http://localhost:3080";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
var argv = args.ToList();

if (argv.Count == 0)
{
    Console.WriteLine("Usage: pm <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  add <title> --assignee <name> [--due-date YYYY-MM-DD] [--requestor <name>] [--priority low|medium|high|critical] [--category <name>] [--description <text>] [--recurrence daily|weekly|monthly|yearly] [--interval <N>]");
    Console.WriteLine("  list [assignee]                 List tasks (optionally filtered by assignee)");
    Console.WriteLine("  start <id>                      Start a task (set status to in_progress)");
    Console.WriteLine("  done <id>                       Complete a task (set status to done)");
    Console.WriteLine("  delete <id>                     Delete a task");
    Console.WriteLine();
    Console.WriteLine("Environment:");
    Console.WriteLine("  PM_API_URL    API base URL (default: http://localhost:3080)");
    return;
}

var command = argv[0].ToLower();

switch (command)
{
    case "add":
        await AddTask(argv);
        break;
    case "list":
        await ListTasks(argv);
        break;
    case "start":
        await UpdateStatus(argv, CoreTaskStatus.InProgress);
        break;
    case "done":
        await UpdateStatus(argv, CoreTaskStatus.Done);
        break;
    case "delete":
        await DeleteTask(argv);
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return;
}

async Task AddTask(List<string> args)
{
    var title = "";
    var assignee = "";
    DateTime? dueDate = null;
    string? requestor = null;
    int priority = 1; // Medium
    string? category = null;
    string? description = null;
    RecurrencePattern? recurrence = null;
    int recurrenceInterval = 1;

    for (int i = 1; i < args.Count; i++)
    {
        if (args[i] == "--assignee" && i + 1 < args.Count)
        {
            assignee = args[i + 1];
            i++; // Skip consumed value
        }
        else if (args[i] == "--due-date" && i + 1 < args.Count)
        {
            if (DateTime.TryParse(args[i + 1], out var parsed))
                dueDate = parsed;
            i++; // Skip consumed value
        }
        else if (args[i] == "--requestor" && i + 1 < args.Count)
        {
            requestor = args[i + 1];
            i++; // Skip consumed value
        }
        else if (args[i] == "--priority" && i + 1 < args.Count)
        {
            var p = args[i + 1].ToLower();
            priority = p switch {
                "low" => 0,
                "medium" => 1,
                "high" => 2,
                "critical" => 3,
                _ => 1
            };
            i++; // Skip consumed value
        }
        else if (args[i] == "--category" && i + 1 < args.Count)
        {
            category = args[i + 1];
            i++; // Skip consumed value
        }
        else if (args[i] == "--description" && i + 1 < args.Count)
        {
            description = args[i + 1];
            i++; // Skip consumed value
        }
        else if (args[i] == "--recurrence" && i + 1 < args.Count)
        {
            var r = args[i + 1].ToLower();
            recurrence = r switch {
                "daily" => RecurrencePattern.Daily,
                "weekly" => RecurrencePattern.Weekly,
                "monthly" => RecurrencePattern.Monthly,
                "yearly" => RecurrencePattern.Yearly,
                _ => null
            };
            i++; // Skip consumed value
        }
        else if (args[i] == "--interval" && i + 1 < args.Count)
        {
            if (int.TryParse(args[i + 1], out var interval))
                recurrenceInterval = interval;
            i++; // Skip consumed value
        }
        else if (!args[i].StartsWith("--"))
        {
            title = args[i];
        }
    }

    if (string.IsNullOrWhiteSpace(title))
    {
        Console.WriteLine("Error: Title is required");
        Console.WriteLine("Usage: pm add <title> --assignee <name> [--due-date YYYY-MM-DD] [--requestor <name>] [--priority low|medium|high|critical] [--category <name>] [--description <text>] [--recurrence daily|weekly|monthly|yearly] [--interval <N>]");
        return;
    }

    if (string.IsNullOrWhiteSpace(assignee))
    {
        Console.WriteLine("Error: Assignee is required");
        Console.WriteLine("Usage: pm add <title> --assignee <name> [--due-date YYYY-MM-DD] [--requestor <name>] [--priority low|medium|high|critical] [--category <name>] [--description <text>] [--recurrence daily|weekly|monthly|yearly] [--interval <N>]");
        return;
    }

    var priorityLevel = (PriorityLevel)priority;
    var request = new { Title = title, Assignee = assignee, DueDate = dueDate, Requestor = requestor, Priority = priorityLevel, Category = category, Description = description, RecurrencePattern = recurrence, RecurrenceInterval = recurrenceInterval, ParentTaskId = (int?)null, NextDueDate = (DateTime?)null };
    var response = await client.PostAsJsonAsync("/api/tasks", request);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        return;
    }

    var task = await response.Content.ReadFromJsonAsync<TaskItem>();
    var dueStr = task!.DueDate.HasValue ? $", due: {task.DueDate.Value:yyyy-MM-dd}" : "";
    var reqStr = !string.IsNullOrEmpty(task.Requestor) ? $", requestor: {task.Requestor}" : "";
    var priStr = task.Priority != PriorityLevel.Medium ? $", priority: {task.Priority}" : "";
    var catStr = !string.IsNullOrEmpty(task.Category) ? $", category: {task.Category}" : "";
    var descStr = !string.IsNullOrEmpty(task.Description) ? $", desc: {task.Description}" : "";
    var recStr = task.RecurrencePattern.HasValue ? $", recurrence: {task.RecurrencePattern} (every {task.RecurrenceInterval})" : "";
    Console.WriteLine($"✓ Task #{task!.Id} created: \"{task.Title}\" (assignee: {task.Assignee}{dueStr}{reqStr}{priStr}{catStr}{descStr}{recStr})");
}

async Task ListTasks(List<string> args)
{
    string? assigneeFilter = null;

    for (int i = 1; i < args.Count; i++)
    {
        if (args[i] == "--assignee" && i + 1 < args.Count)
        {
            assigneeFilter = args[i + 1];
            i++; // Skip consumed value
        }
        else if (!args[i].StartsWith("--"))
        {
            assigneeFilter = args[i];
        }
    }

    var url = "/api/tasks";
    if (!string.IsNullOrWhiteSpace(assigneeFilter))
        url += $"?assignee={Uri.EscapeDataString(assigneeFilter)}";

    var response = await client.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        return;
    }

    var tasks = await response.Content.ReadFromJsonAsync<List<TaskItem>>();

    if (tasks == null || tasks.Count == 0)
    {
        Console.WriteLine("No tasks found.");
        return;
    }

    foreach (var task in tasks)
    {
        var statusIcon = task.Status switch
        {
            CoreTaskStatus.Todo => "[ ]",
            CoreTaskStatus.InProgress => "[~]",
            CoreTaskStatus.Done => "[✓]",
            _ => "[?]"
        };
        var dueStr = task.DueDate.HasValue ? $" | due: {task.DueDate.Value:yyyy-MM-dd}" : "";
        var reqStr = !string.IsNullOrEmpty(task.Requestor) ? $" | requestor: {task.Requestor}" : "";
        var priStr = task.Priority != PriorityLevel.Medium ? $" | prio: {task.Priority}" : "";
        var catStr = !string.IsNullOrEmpty(task.Category) ? $" | cat: {task.Category}" : "";
        var descStr = !string.IsNullOrEmpty(task.Description) ? $" | desc: {task.Description}" : "";
        var recStr = task.RecurrencePattern.HasValue ? $" | rec: {task.RecurrencePattern} (×{task.RecurrenceInterval})" : "";
        Console.WriteLine($"{statusIcon} #{task.Id} | {task.Status,-12} | {task.Assignee,-10} | {task.Title}{dueStr}{reqStr}{priStr}{catStr}{descStr}{recStr}");
    }
}

async Task UpdateStatus(List<string> args, CoreTaskStatus newStatus)
{
    if (args.Count < 2)
    {
        Console.WriteLine($"Error: Task ID required");
        Console.WriteLine($"Usage: pm {(newStatus == CoreTaskStatus.Done ? "done" : "start")} <id>");
        return;
    }

    if (!int.TryParse(args[1], out var id))
    {
        Console.WriteLine($"Error: Invalid task ID: {args[1]}");
        return;
    }

    var request = new { Status = newStatus };
    var response = await client.PatchAsJsonAsync($"/api/tasks/{id}", request);

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        Console.WriteLine($"Error: Task #{id} not found");
        return;
    }

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        return;
    }

    var statusName = newStatus switch
    {
        CoreTaskStatus.Todo => "todo",
        CoreTaskStatus.InProgress => "in_progress",
        CoreTaskStatus.Done => "done",
        _ => "unknown"
    };

    Console.WriteLine($"✓ Task #{id} → {statusName}");
}

async Task DeleteTask(List<string> args)
{
    if (args.Count < 2)
    {
        Console.WriteLine("Error: Task ID required");
        Console.WriteLine("Usage: pm delete <id>");
        return;
    }

    if (!int.TryParse(args[1], out var id))
    {
        Console.WriteLine($"Error: Invalid task ID: {args[1]}");
        return;
    }

    var response = await client.DeleteAsync($"/api/tasks/{id}");

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        Console.WriteLine($"Error: Task #{id} not found");
        return;
    }

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        return;
    }

    Console.WriteLine($"✓ Task #{id} deleted");
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Assignee { get; set; } = "";
    public string? Requestor { get; set; }
    public CoreTaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public PriorityLevel Priority { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
    public int RecurrenceInterval { get; set; }
    public int? ParentTaskId { get; set; }
    public DateTime? NextDueDate { get; set; }
}