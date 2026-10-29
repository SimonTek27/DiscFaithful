# Controlli – Disc Faithful

## Gamepad Xbox (fedele all'originale)

| Azione | Pulsante |
|---|---|
| Muovi | Left Stick |
| Mira (lancio direzionale) | Right Stick |
| Lancia | RT / A |
| Para / Prendi | X / LB / LT (tenuti) |
| Menu: su/giù | D-Pad Up/Down |
| Menu: conferma | A |
| Menu: indietro | B |

- **Parata**: tenendo premuto i pulsanti di para, se il disco ti colpisce
  **lo catturi** (regola originale: *parata = prendi il disco*).
- Disc in mano: non puoi lanciare per 0.45 s dopo il lancio precedente (cooldown).
- Aim con Right Stick: la direzione stick sovrascrive il facing.
  Stick centrale = lancia dritto davanti a te.

## Tastiera (fallback: nessun controller collegato)

Il fallback tastiera scatta **automaticamente** se non c'è un gamepad
per quel `PlayerIndex`. Con il pad collegato la tastiera è ignorata (tranne il menu).

| Azione | Player 1 | Player 2 |
|---|---|---|
| Muovi | W / A / S / D | ↑ / ← / ↓ / → |
| Mira (ruota tu stesso) | Q / E | O / P |
| Lancia | Spazio | Invio |
| Para / Prendi (tenuto) | Shift sx | Shift dx |
| Menu conferma | Invio / Spazio / A (pad) |  |

Note tastiera:
- Il facing ruota **quando ti muovi**; con Q/E/O/P puoi mirare da fermo
  (≈290°/s) e lanciare dove vuoi.
- Parata cattura: tenendo Shift, il disco che ti colpisce passa in mano.
- `ModeSelectUI` risponde a ↑/↓ + Invio/Spazio anche col pad attivo.

## 2 giocatori

- `Player1.PlayerIndex = 0`, `Player2.PlayerIndex = 1`
- Con un solo pad? P2 finisce su tastiera **solo se `GamePadCount < 2`**.
  Consiglio: 1 player umano + CPU (`AiControlled = true` su Player2 + script `AiOpponent`).

## Input della CPU

Non c'è input "fisico": `AiOpponent` chiama ogni frame
`PlayerController.SetAiInput(move, blocking, wantThrow, throwDir)` che fa
da pad virtuale. Vedi `src/ai/AiOpponent.cs`.
