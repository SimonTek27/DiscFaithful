# DISC Faithful – Clone 100% originale in Stride (3D + Xbox)

Remake fedele delle meccaniche di **Disc (Loriciel, 1990/91)** realizzato in Stride Engine.

## Struttura del progetto

```
src/
  Tile.cs
  Platform.cs
  Disc.cs
  GameManager.cs
  CameraController.cs
  AudioManager.cs
  controls/
    PlayerController.cs          ← Xbox + movimento + animazioni
  ui/                            ← HUD 1990-style, menu, score (in arrivo)
  multiplayer/                   ← netcode / online (in arrivo)
assets/                          ← modelli 3D, texture, audio
Docs/
  00_ORIGINAL_RULES.md
  01_STRIDE_SETUP.md
  02_XBOX_CONTROLS.md
  03_3D_MODELS_GUIDE.md
  04_FREE_3D_ASSETS.md
  05_STRIDE_IMPORT_GUIDE.md
```

## Avvio rapido

1. Crea un nuovo progetto **3D** in Stride Game Studio.
2. Copia la cartella `src` nel progetto (o aggiungi i file come script).
3. Segui `Docs/05_STRIDE_IMPORT_GUIDE.md`.
4. Assegna i riferimenti in editor.
5. Premi Play e usa un controller Xbox.

## Controlli Xbox

| Azione        | Pulsante     |
|---------------|--------------|
| Muovi         | Left Stick   |
| Mira          | Right Stick  |
| Lancia        | RT / A       |
| Para / Prendi | X / LB / LT  |

## Regole fedeli all'originale

- 8 floor + 8 wall per piattaforma (griglia 2×4)
- Danno solo sulle wall → ciclo simboli → distruzione wall + floor collegata
- Colpo al corpo = -1 vita
- Parata = prendi il disco
- Vittoria: vita a 0 **oppure** nessuna floor rimasta
- Fino a 4 dischi nei livelli alti

## Asset free consigliati

Vedi `Docs/04_FREE_3D_ASSETS.md` (Quaternius + Kenney – CC0).
