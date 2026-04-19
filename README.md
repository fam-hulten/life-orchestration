# Life Orchestration

Personal task management tool – används för att hantera todos som inte hör hemma som GitHub issues.

## Quick Start

**CLI:**
```bash
# Ladda ner från releases: https://github.com/fam-hulten/life-orchestration/releases/tag/v1.0.0
export PM_API_URL=http://192.168.1.194:3080
./LifeOrchestration.Cli list
./LifeOrchestration.Cli add "Min task" --assignee robert
```

**API:**
```
GET  http://192.168.1.194:3080/api/tasks
POST http://192.168.1.194:3080/api/tasks  {"title":"...", "assignee":"..."}
```

## När ska jag använda detta vs GitHub Issues?

| GitHub Issues | life-orchestration |
|---------------|-------------------|
| Formella features/bugs | Snabba todos |
| Kräver diskussion | Självförklarande uppgifter |
| Långlivade tasks | Kortlivade uppgifter |
|需要tracking | Fixa-saker-nu |

## Stack

- **API:** .NET 9 + Minimal API + SQLite
- **CLI:** .NET 9 + HttpClient
- **GUI:** Blazor Server (kommande)