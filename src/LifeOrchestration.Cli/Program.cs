using System.Net.Http.Json;
using CoreTaskStatus = LifeOrchestration.Core.Entities.TaskStatus;

var baseUrl = Environment.GetEnvironmentVariable("PM_API_URL") ?? "http://localhost:3080";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
var args = args.ToList();

if (args.Count == 0)
{
    Console.WriteLine("Usage: pm <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  add <title> --assignee <name>    Add a new task");
    Console.WriteLine("  list [assignee]                 List tasks (optionally filtered by assignee)");
    Console.WriteLine("  start <id>                      Start a task (set status to in_progress)");
    Console.WriteLine("  done <id>                       Complete a task (set status to done)");
    Console.WriteLine("  delete <id>                     Delete a task");
    Console.WriteLine();
    Console.WriteLine("Environment:");
    Console.WriteLine("  PM_API_URL    API base URL (default: http://localhost:3080)");
    return;
}

var command = args[0].ToLower();

switch (command)
{
    case "add":
        await AddTask(args);
        break;
    case "list":
        await ListTasks(args);
        break;
    case "start":
        await UpdateStatus(args, CoreTaskStatus.InProgress);
        break;
    case "done":
        await UpdateStatus(args, CoreTaskStatus.Done);
        break;
    case "delete":
        await DeleteTask(args);
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return;
}

async Task AddTask(List<string> args)
{
    var title = "";
    var assignee = "";

    for (int i = 1; i < args.Count; i++)
    {
        if (args[i] == "--assignee" && i + 1 < args.Count)
            assignee = args[i + 1];
        else if (!args[i].StartsWith("--"))
            title = args[i];
    }

    if (string.IsNullOrWhiteSpace(title))
    {
        Console.WriteLine("Error: Title is required");
        Console.WriteLine("Usage: pm add <title> --assignee <name>");
        return;
    }

    if (string.IsNullOrWhiteSpace(assignee))
    {
        Console.WriteLine("Error: Assignee is required");
        Console.WriteLine("Usage: pm add <title> --assignee <name>");
        return;
    }

    var request = new { Title = title, Assignee = assignee };
    var response = await client.PostAsJsonAsync("/api/tasks", request);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        return;
    }

    var task = await response.Content.ReadFromJsonAsync<TaskItem>();
    Console.WriteLine($"✓ Task #{task!.Id} created: \"{task.Title}\" (assignee: {task.Assignee})");
}

async Task ListTasks(List<string> args)
{
    string? assigneeFilter = null;

    for (int i = 1; i < args.Count; i++)
    {
        if (args[i] != "--assignee" && !args[i].StartsWith("--"))
            assigneeFilter = args[i];
        else if (args[i] == "--assignee" && i + 1 < args.Count)
            assigneeFilter = args[i + 1];
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
        Console.WriteLine($"{statusIcon} #{task.Id} | {task.Status,-12} | {task.Assignee,-10} | {task.Title}");
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
    public CoreTaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
