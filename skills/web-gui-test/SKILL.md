---
name: web-gui-test
description: Test web GUIs using browser automation. Use when asked to verify a web interface works, test buttons/forms, or debug GUI issues. Requires Playwright-capable browser running on host. Workflow: (1) Open URL with browser tool, (2) Take snapshot to get element refs, (3) Interact using act with refs, (4) Verify results. Only use refs from most recent snapshot.
---

# Web GUI Testing

## Verktyg

OpenClaw browser tool ( Playwright-baserad) på `target=host` (lokal Windows/Wi
```bash
browser(action="start", profile="user")
```

## Steg-för-Steg

### 1. Öppna sidan
```
browser(action="open", url="http://target-url.com")
```
Spara `targetId` från svaret.

### 2. Snapshot för att hitta element
```
browser(action="snapshot", targetId="<id>")
```
**Viktigt:** Använd refs bara från senaste snapshot. Refs ändras efter varje interaktion!

### 3. Interagera
```
browser(action="act", targetId="<id>", ref="<ref>", kind="click")
browser(action="act", targetId="<id>", ref="<ref>", kind="type", text="Text hier")
browser(action="act", targetId="<id>", ref="<ref>", kind="select", values=["OptionValue"])
```

### 4. Verifiera ändringar
- Ta ny snapshot
- Kolla att nytt innehåll syns
-Verifiera via API med curl:
```bash
curl -s http://192.168.1.194:3080/api/tasks | python3 -c "import json,sys; t=json.load(sys.stdin); print(f'Total: {len(t)}')"
```

## Vanliga fel

- **"tab not found"**: Använd fel targetId – kolla senaste öppna/snapshot
- **"Unknown ref"**: Nova snapshot behövs – refs ändrades
- **JavaScript funkar inte**: Kolla med `browser(action="console", targetId="<id>")`

## Verifiera Create/Update/Delete

### Create:
1. Fyll i formulär
2. Klicka "Skapa"
3.Verifiera API:
```bash
curl -s http://192.168.1.194:3080/api/tasks | python3 -c "import json,sys; t=json.load(sys.stdin); print([x['title'] for x in t if 'test' in x['title'].lower()])"
```

### Update:
1. Klicka på status-knapp (t.ex. ✓ för done)
2. Verifiera status ändrades i API

### Delete:
1. Klicka på ✕-knapp
2.Verifiera task försvann från lista och API

## Debug-tips

- Screenshot utan att öppna: `browser(action="screenshot", url="...")`
- Läs JavaScript-fel: `browser(action="console", targetId="<id>")`
- Om API inte svara, kolla CORS headers:
```bash
curl -s -I http://192.168.1.194:3080/api/tasks -H "Origin: http://192.168.1.194:3081"
```
