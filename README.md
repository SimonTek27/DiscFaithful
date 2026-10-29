# DISC Faithful – Clone 100% originale in Stride (3D + Xbox)

Remake fedele delle meccaniche di **Disc (Loriciel, 1990/91)** realizzato in Stride Engine.

## Contenuto

```
Scripts/
  Tile.cs              – piastrella floor/wall + simboli + animazioni Hit/Destroy
  Platform.cs          – 8+8 tile, link 1:1, auto-collect
  Disc.cs              – fisica, possesso, rotazione, trail, collisioni
  PlayerController.cs  – Xbox pad, movimento 3D, clamp floor, animazioni
  GameManager.cs       – livelli, multi-disco (1→4), score, reset round
  CameraController.cs  – camera fissa stile arcade 3D
  AudioManager.cs      – wrapper per i sample originali

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
2. Copia la cartella `Scripts` nel progetto.
3. Segui `Docs/01_STRIDE_SETUP.md` e `Docs/05_STRIDE_IMPORT_GUIDE.md`.
4. Assegna i riferimenti in editor (liste tile, PlayerIndex, DiscPrefab, Camera…).
5. Premi Play e usa un controller Xbox.

## Controlli

| Azione        | Xbox              |
|---------------|-------------------|
| Muovi         | Left Stick        |
| Mira          | Right Stick       |
| Lancia        | RT / A            |
| Para / Prendi | X / LB / LT       |

## Regole fedeli

- 8 floor + 8 wall per piattaforma (griglia 2×4)
- Danno solo sulle wall → ciclo simboli → distruzione wall + floor collegata
- Colpo al corpo = -1 vita
- Parata = prendi il disco
- Vittoria: vita a 0 **oppure** nessuna floor rimasta
- Fino a 4 dischi nei livelli alti

## Asset free consigliati

Vedi `Docs/04_FREE_3D_ASSETS.md` (Quaternius + Kenney CC0).
