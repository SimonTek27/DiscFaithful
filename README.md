# DISC Faithful – Clone 100% originale in Stride (3D + Xbox)

Remake fedele delle meccaniche di **Disc (Loriciel, 1990/91)** realizzato in Stride Engine.

## Struttura del progetto

```
src/
  Tile.cs
  Platform.cs
  Disc.cs
  GameManager.cs
  GameMode.cs
  CameraController.cs
  AudioManager.cs
  controls/
    PlayerController.cs          ← Xbox + tastiera fallback + input CPU + animazioni
  ai/
    AiOpponent.cs                ← avversario CPU, difficoltà scalata col livello
  ui/
    GameHud.cs                   ← HUD 1990-style, score, vita, messaggi
    ModeSelectUI.cs              ← menu modalità (TRAINING/SINGLE/TOURNAMENT/CHAMPIONSHIP)
assets/                          ← modelli 3D, texture, audio
Docs/
  00_ORIGINAL_RULES.md
  01_STRIDE_SETUP.md             ← cablaggio scena completo
  02_XBOX_CONTROLS.md            ← pad + tastiera
  03_3D_MODELS_GUIDE.md
  04_FREE_3D_ASSETS.md
  05_STRIDE_IMPORT_GUIDE.md
```

## Avvio rapido

1. Crea un nuovo progetto **3D** in Stride Game Studio.
2. Copia la cartella `src` nel progetto (o aggiungi i file come script).
3. Segui `Docs/01_STRIDE_SETUP.md` (cablaggio scena + riferimenti).
4. Assegna i riferimenti in editor, `GameManager.AutoStart = false` per partire dal menu.
5. Premi Play e usa un controller Xbox — oppure WASD+Spazio+Shift se non c'è pad.

## Controlli Xbox

| Azione | Pulsante |
|--------|----------|
| Muovi | Left Stick |
| Mira | Right Stick |
| Lancia | RT / A |
| Para / Prendi | X / LB / LT |
| Menu | D-Pad + A |

Fallback tastiera: P1 = WASD/QE/Spazio/Shift sx, P2 = frecce/OP/Invio/Shift dx.
Vedi `Docs/02_XBOX_CONTROLS.md`.

## Avversario CPU

`src/ai/AiOpponent.cs`: para i dischi in arrivo (la parata cattura il disco,
come nell'originale), schiva, si posiziona al centro delle floor vive e lancia
con errore di mira che scende col livello. La difficoltà è derivata da
`GameManager.CurrentLevel` (vedi `Docs/01_STRIDE_SETUP.md` §5).

## Regole fedeli all'originale

- 8 floor + 8 wall per piattaforma (griglia 2×4), corrispondenza 1:1
- Danno solo sulle wall → ciclo simboli (Pentagon→Square→Triangle→=→Circle→distruzione wall + floor collegata)
- Colpo al corpo = −1 vita
- Parata = prendi il disco
- Vittoria: vita a 0 **oppure** nessuna floor rimasta
- Fino a 4 dischi nei livelli alti
- Disco uscito dall'arena o perso → restituito al suo ultimo proprietario

## Modalità

| Modalità | Regole |
|---|---|
| TRAINING | vita infinita, reset manuale, no score |
| SINGLE MATCH | al meglio dei 3 round |
| TOURNAMENT | avanzamento livelli 1-6, dischi in crescita, campione finale |
| CHAMPIONSHIP | serie di match (best of 5), livelli 1-8 |

Fine round automatico (3 s); a modalità completata → "PRESS A FOR MENU".

## Asset free consigliati

Vedi `Docs/04_FREE_3D_ASSETS.md` (Quaternius + Kenney – CC0).
