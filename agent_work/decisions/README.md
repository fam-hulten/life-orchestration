# Decision Logger

**Datum:** 2026-04-23  
**Status:** KLAR  
**Byggare:** Robert  
**Dokumenterare:** Lilly

## Vad

CLI-verktyg för att logga, lista och söka bland beslut.

## Kommandon

```bash
decisions add "Beslutstext" --context "Bakgrund"
decisions list
decisions search "term"
```

## Tekniskt

- **Språk:** Python 3
- **Databas:** SQLite (`decisions.db`)
- **Plats:** `agent_work/decisions/`

## Resultat

- ✅ CLI funkar
- ✅ 3 beslut dokumenterade
- ✅ Sök funkar

## Learnings

- Python + SQLite = snabbare än .NET för små CLI-verktyg
- next step: lägga till i PATH så `decisions` funkar från var som helst
