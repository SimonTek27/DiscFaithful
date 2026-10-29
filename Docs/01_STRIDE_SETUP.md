# Stride Setup – Disc Faithful

Guida completa per creare il progetto Stride e cablare tutti gli script.

## 1. Creazione progetto

1. Scarica **Stride Game Studio** (consigliato 4.1+, .NET 8): https://www.stride3d.net/download/
2. `Nuovo progetto → 3D (Game)` – nome `DiscFaithful`, fullscreen **off** durante lo sviluppo.
3. Sistema di riferimento consigliato: **1 unità = 1 metro**, tile ≈ 1.0 × 1.0 m.

## 2. Struttura del codice

Copia la cartella `src/` del repo nel progetto. Stride compila automaticamente i `.cs` di `MyGame` — verifica in **Game Studio → Code** che tutte le classi appaiano:

```
GameManager, GameMode, Platform, Tile, Disc, CameraController,
AudioManager, PlayerController, AiOpponent
GameHud, ModeSelectUI (namespace DiscFaithful.UI)
```

Singleton pattern usato: ogni script è un componente da aggiungere a un Entity in editor.

## 3. Gerarchia scena consigliata

```
Root
├── ArenaCenter (Entity vuoto, Position 0,0,0)
├── GameManager (Entity vuoto)
│     └── GameManager (SyncScript)
├── Platform1 (Entity, Position 0, 0, -8)
│     ├── Floor_0 … Floor_7   (griglia 2×4, Index 0-7)
│     └── Wall_0  … Wall_7    (dietro ogni floor, Index 0-7)
├── Platform2 (Entity, Position 0, 0, +8, ruotata 180° su Y)
│     └── (stessa struttura platform 1)
├── Player1 (Entity + Model + Rigidbody Character/StaticCollider)
│     └── PlayerController (SyncScript, PlayerIndex 0)
├── Player2 (Entity + Model collassato o CPU)
│     ├── PlayerController (SyncScript, PlayerIndex 1, AiControlled = true)
│     └── AiOpponent (SyncScript)
├── Camera (Entity + CameraComponent + CameraController)
├── AudioManager (Entity vuoto)
│     └── AudioManager (StartupScript, Sound = asset audio)
├── HUD (Entity con UIComponent)
│     └── GameHud (SyncScript)
├── Menu (Entity con UIComponent **separato**)
│     └── ModeSelectUI (SyncScript)
└── DiscPrefab (Prefab: Entity + CylinderModel + RigidbodyComponent sphere)
```

## 4. Configurazione componenti chiave

### Tiles (Floor / Wall)

Ogni tile è un Entity figlio della piattaforma:
- `ModelComponent` con cubo Kenney Prototype Kit
- `StaticColliderComponent` (shape box) — floor: collision filter `Ground`; wall: `World`
- Script `Tile`:
  - `Type` = Floor oppure Wall
  - `Index` = riga*4+colonna (0-7) — **deve corrispondere 1:1 tra floor e wall collegati**
  - `SymbolRoot` = Entity figlio `Symbol` con figli `Pentagon`, `Square`, `Triangle`, `Equals`, `Circle` (modelli/sprite; uno visibile per volta)

### Player

- `ModelComponent` (Quaternius CC0, vedi `03_3D_MODELS_GUIDE.md`)
- `AnimationComponent` con clip: `Idle`, `Walk`, `Throw`, `Block`, `Hit`, `Fall`, `Victory`
- Collider: static kinematic, così il disco lo rileva (`OnHitPlayer`)
- Script `PlayerController`:
  - `PlayerIndex`: 0 = P1, 1 = P2
  - `AiControlled`: true per la CPU
  - `MyPlatform`: piattaforma sotto i piedi
- Se P2 è CPU → aggiungi anche `AiOpponent` (vedi §5)

### Disc prefab

- Entity: `DiscRoot`
  - `RigidbodyComponent` (dynamic, sphere collider r≈0.18)
  - Phys material: **Restitution 0.98, Friction 0.1**
  - `ModelComponent` (cylinder n. di dimensioni frisbee o `assets/items/frisbee.fbx`)
  - Child `Trail` con `ElementEffect/Trail` (disabile di default)
  - Script `Disc`
- Salvatelo come **Prefab** `Disc.prefab`

### GameManager — riferimenti da assegnare

| Campo | Assegna |
|---|---|
| `Player1` / `Player2` | Entity Player 1 / 2 |
| `Platform1` / `Platform2` | Entity piattaforme |
| `DiscPrefab` | prefab `Disc` |
| `Audio` | Entity AudioManager |
| `OpponentAi` | componente AiOpponent (in P2, se CPU) |
| `AutoStart` | **false** se usi il menu; la partita parte solo alla selezione |
| `RoundRestartDelay` | 3 secondi (1.5 nei tornei, a gusto) |

### GameHud / ModeSelectUI

- `HUD` Entity: `UIComponent` (la pagina la crea lo script runtime) + `GameHud`:
  - `GameManager`, `Menu` → Entity Menu (così al champion riparti al menu con A).
- `Menu` Entity: **UIComponent proprio** + `ModeSelectUI` → assegna `GameManager`.
  ⚠ Importante: un UIComponent per script, non condiviso — entrambi creano la pagina runtime.

## 5. AiOpponent — config

| Campo | Default | Note |
|---|---|---|
| `Self` | Player2 | giocatore controllato dalla CPU |
| `Opponent` | Player1 | bersaglio dei lanci |
| `Manager` | GameManager | per tracciare i dischi in volo |
| `MyPlatform` | Platform2 | per evitare le floor distrutte |
| `UseManualDifficulty` | false | true = manuali i valori sotto |
| BlockChancePerSecond | scala da livello | prob/s di avviare la parata |
| AimErrorDegrees | 14°→1.5° | imprecisione che scende col livello |
| ThrowDelayMin/Max | 1.1→0.25 / 2.2→0.6 s | ritardo lancio |

La difficoltà si auto-regola su `GameManager.CurrentLevel`
(chiamando `SetDifficultyFromLevel`; lo fa GameManager a ogni cambio livello).

## 6. Fisica

- Collision Groups consigliati:
  - `Player` (personaggi) — StaticCollider kinematic
  - `Disc` (prefab)
  - `Ground` (tiles floor)
  - `World` (tiles wall + SideWall entity)
- `SideWall`: entity perimetrale con `StaticCollider` + marker script `SideWall` (già in `Disc.cs`).
  Restituzione 1.0 = rimbalzo laterale fedele all'originale.
- Disc: `CcdMotionThreshold 0.1`, `CcdSweptSphereRadius 0.3` → no tunneling a 14 m/s.

## 7. Checklist Play

- [ ] Premi Play: se `AutoStart=false` appare il menu stile 1990
- [ ] Si muove il P1 con stick/WASD
- [ ] La CPU si muove, para, lancia (errore di mira visibile a Lv1)
- [ ] Colpo wall: simbolo degrada → wall+floor distrutte
- [ ] Caduta sotto l'arena quando le floor sotto di te sono tutte distrutte
- [ ] Round finito → dopo 3 s riapre il round; fine modalità → "PRESS A FOR MENU"
- [ ] Audio: gong a inizio round, impact/block/tile_destroy in gioco
