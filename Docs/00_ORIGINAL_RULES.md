# DISC (Loriciel 1990) – Regole 100% fedeli + implementazione 3D

## Arena
- Due piattaforme sospese una di fronte all’altra (3D).
- Ogni piattaforma: 8 floor + 8 wall (griglia 2×4), corrispondenza 1:1.

## Simboli di danno (wall tiles)
| Colpi | Simbolo |
|-------|--------|
| 5 | Pentagono |
| 4 | Quadrato |
| 3 | Triangolo |
| 2 | = |
| 1 | Cerchio |
| 0 | Distrutta (+ floor collegata) |

## Disco
- Possesso, rimbalzo laterali (Restitution 1.0), colpo corpo = -1 vita, parata = prendi disco.
- Fino a 4 dischi nei livelli alti.

## Vittoria
Vita ≤ 0 oppure nessuna floor rimasta sotto il giocatore.

## Controlli Xbox
Left Stick = muovi, Right Stick = mira, RT/A = lancia, X/LB/LT = para.
