# Morgonrapport-mall

Använd denna mall för morgonrapporter till Johanna.

## Format

```
📅 **IGÅR (DD MMM)**
- repo: commit-meddelande
- repo: commit-meddelande
- experiments: Kort beskrivning av experiment (om kör)

🔨 **IDAG**
- [Planerade uppgifter – 3-5 punkter max]
- [Prioritet om viktigt]
- pm list --assignee lilly (relevanta tasks)

⚠️ **BLOCKER**
- [Eventuella hinder – eller "Inga blocker"]
```

## Regler

- Kortast möjligt – varje punkt ≤ 10 ord
- Max 3-5 idag-punkter
- Ta med blockers även om tomma
- Inkludera experiment om ni körde något
- pm list --assignee lilly = bra startpunkt för "idag"

## Undvik

- Förklaringar och bakgrund
- Fylliga meningar
- Mer än 500 ord totalt
- Tekniska detaljer ( Discord = scanningsbart)

## Exempel

```
📅 **IGÅR (22 APR)**
- standup-generator: Add Swedish date formatting
- agent_work: Document 4 experiments
- experiments: Morning Report Template

🔨 **IDAG**
- Köra morgonrapporter med ny mall
- Testa pm-mvp workflow states
- Följa upp Johannas feedback

⚠️ **BLOCKER**
- Inget just nu
```
