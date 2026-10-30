# UI – stile contemporaneo

Menu e HUD moderni: layout pulito, tipografia chiara, palette scura con accent blu.

## Design

| Elemento | Scelta |
|----------|--------|
| Sfondo | `#0C0E14` scuro semi-trasparente |
| Card / panel | `#181C26` |
| Testo primario | `#F5F7FA` |
| Testo secondario | `#A0AABE` |
| Accent | `#588CFF` |
| Pericolo (vita bassa) | `#FF5A5A` |

Niente pixel art / terminal green: look da gioco 2020+.

## Script

| File | Ruolo |
|------|--------|
| `ModeSelectUI.cs` | Menu Training / Single Match / Tournament / Championship |
| `GameHud.cs` | Score, vita, livello, messaggi round |

## Setup Stride

1. Entity `UI_ModeSelect` → `UIComponent` + `ModeSelectUI` → collega `GameManager`
2. Entity `UI_HUD` → `UIComponent` + `GameHud` → collega `GameManager`
3. Font: sans-serif di sistema o un font UI moderno (es. Inter / Roboto se importato)

## Controlli menu

- **↑ ↓** / D-Pad — seleziona
- **A** / Enter — conferma

## API

```csharp
modeSelect.ShowMenu();
modeSelect.HideMenu();
hud.ShowMessage("Custom text", 2f);
```
