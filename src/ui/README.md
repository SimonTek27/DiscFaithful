# UI – stile 1990 (Amiga / DOS)

## Regole visive
- Niente UI moderne, flat o minimal
- Colori terminali: verde `#00DC00`, giallo `#FFDC00`, sfondo blu scuro
- Testo a blocchi maiuscolo
- Navigazione pad: D-Pad + A / B

## Script

| File | Ruolo |
|------|--------|
| `ModeSelectUI.cs` | Menu TRAINING / SINGLE MATCH / TOURNAMENT / CHAMPIONSHIP |
| `GameHud.cs` | Score, vita, livello, messaggi round |

## Setup in Stride
1. Crea Entity `UI_ModeSelect` con `UIComponent` + `ModeSelectUI`
2. Assegna riferimento a `GameManager`
3. Crea Entity `UI_HUD` con `UIComponent` + `GameHud`
4. Font: usa un font pixel/monospace se disponibile (es. "Press Start" style o default mono)

## Controlli menu
- **Up / Down** – seleziona modalità
- **A / Enter** – avvia
- **B** – indietro (estendibile)
