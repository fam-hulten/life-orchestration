# TOOLS.md - Local Notes

Skills define _how_ tools work. This file is for _your_ specifics — the stuff that's unique to your setup.

## What Goes Here

Things like:

- Camera names and locations
- SSH hosts and aliases
- Preferred voices for TTS
- Speaker/room names
- Device nicknames
- Anything environment-specific

## Examples

```markdown
### Cameras

- living-room → Main area, 180° wide angle
- front-door → Entrance, motion-triggered

### SSH

- home-server → 192.168.1.100, user: admin

### TTS

- Preferred voice: "Nova" (warm, slightly British)
- Default speaker: Kitchen HomePod
```

## Why Separate?

Skills are shared. Your setup is yours. Keeping them apart means you can update skills without losing your notes, and share skills without leaking your infrastructure.

---

## Projects

### life-orchestration
Personal task and life orchestration tool for Johanna, Robert and Lilly.

**Repo:** https://github.com/fam-hulten/life-orchestration

**API:** `http://192.168.1.194:3080`
**CLI:** https://github.com/fam-hulten/life-orchestration/releases/tag/v1.0.0

**Använd för todos som inte hör hemma som GitHub issues** – det är notrepositoryt för snabb taskhantering utan att skapa issues.

**CLI-kommandon:**
```bash
pm add "Titel" --assignee <robert|lilly|johanna>
pm list
pm list --assignee <name>
pm start <id>
pm done <id>
pm delete <id>
```

**API endpoints:**
- `GET /api/tasks` – lista alla tasks
- `POST /api/tasks` – skapa task `{title, assignee}`
- `PATCH /api/tasks/:id` – uppdatera status `{status}`
- `DELETE /api/tasks/:id` – ta bort task

**Stack:** .NET 9 + SQLite + Minimal API

---

Add whatever helps you do your job. This is your cheat sheet.
