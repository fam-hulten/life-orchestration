# Life Orchestration

Personal task and life orchestration tool.

## Quick Start

```bash
# Start API
docker-compose up -d

# API: http://localhost:3080
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/tasks | List all tasks |
| GET | /api/tasks/{id} | Get task by ID |
| POST | /api/tasks | Create task |
| PATCH | /api/tasks/{id} | Update task status |
| DELETE | /api/tasks/{id} | Delete task |

## Task Schema

```json
{
  "id": 1,
  "title": "Task title",
  "assignee": "Robert",
  "status": "Todo",
  "createdAt": "2026-04-19T17:00:00Z"
}
```

## Status Values

- `Todo` (0)
- `InProgress` (1)
- `Done` (2)
