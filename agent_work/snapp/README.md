# Snäpp – Widget-bildgenerator (MVP)

## Vad är detta?

En enkel webbtjänst för att generera widget-bilder med AI. Skriv ett koncept (t.ex. "gunga") och få en bild.

## Tech stack

- **Frontend**: Vanilla HTML/CSS/JS (ingen bundler)
- **Backend**: Node.js + Express
- **Bild-AI**: MiniMax via `mmx` CLI
- **Lagring**: Lokal JSON-cache + `generated/`-mapp

## Kom igång

```bash
cd agent_work/snapp
npm install
mmx auth --api-key <DIN_NYCKEL>   # om du inte redan är inloggad
npm start
# Öppna http://localhost:3081
```

## Miljövariabler

Inga magic-strängar — `mmx` CLI läser auth från sin egen config.

## MVP-limitations

- Ingen databas (bara JSON-cache)
- Ingen Concepta-integration (Steg 2)
- Ingen användarhantering
- Ingen bild-redigering efter generering
