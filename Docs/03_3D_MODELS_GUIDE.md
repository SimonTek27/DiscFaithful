# Modelli 3D – Disc Faithful

Cosa serve, dimensioni, pivot e come si usa. Tutto CC0 o uso ok.

## 1. Personaggi (Quaternius + Mixamo)

**Fonte**: Quaternius Ultimate Animated Character Pack (CC0)
https://quaternius.com/packs/ultimatedanimatedcharacter.html

- 1 personaggio basta, lo riusa per P1 e P2 (P2 di colore diverso via material).
- Scale target: **1.8 m di altezza**, pivot ai piedi (0, 0, 0).
- Import in `assets/characters/`, material alternativo per P2 (es. tinta rossa).

### Animazioni richieste dai nomi in PlayerController

| Clip | Loop | Momento |
|---|---|---|
| `Idle` | sì | fermo |
| `Walk` | sì | movimento stick |
| `Throw` | no | lancio disco |
| `Block` | sì | parata tenuta |
| `Hit` | no | colpo al corpo (−1 vita) |
| `Fall` | no | morte / caduta |
| `Victory` | sì | vinto il round |

Mixamo per le clip mancanti (Throw/Block/Hit/Fall):
https://www.mixamo.com – esporta FBX **senza skin**, importa nel progetto e
rinomina le clip nella `AnimationComponent` con i nomi indicati qui sopra.

Se mancano clip, i nomi non presenti in `Animations` sono semplicemente ignorati
(`PlayAnim` fa un ContainsKey) → il gioco funziona comunque T-pose.

## 2. Piastrelle (Kenney Prototype Kit)

**Fonte**: https://kenney.nl/assets/prototype-kit (CC0)

- 1 mesh cubo per **Floor** e 1 per **Wall** (spessore 10-15 cm per le wall).
- Size: floor 1.0 × 0.1 × 1.0; wall 1.0 × 0.6 × 0.12.
- Posizione wall: disposta **dietro** ogni floor, faccia frontale verso il centro arena.
- Materiali: floor = grigio scuro; wall = blu scuro con leggero emissive per
  far risaltare i simboli.

### Simboli di danno (figli di `Symbol`)

Nomi esatti richiesti da `Tile.GetCurrentSymbol()` (case-sensitive):

| Colpi restanti | Nome child |
|---|---|
| 5 | `Pentagon` |
| 4 | `Square` |
| 3 | `Triangle` |
| 2 | `Equals` |
| 1 | `Circle` |
| 0 | `Destroyed` (o nulla: la palloncella si distrugge) |

Realizzazione allowed: sprite (SPK) oppure mesh sottili estruse.
`UpdateSymbolVisual()` attiva fsolo il child col nome corrente.

## 3. Disco

**Opzione consigliata**: Cylinder nativo Stride (nessun import):
- raggio ~0.18 m, altezza 0.05, pivot centro.
- Il disco ruota con `SpinSpeed` (720°/s) → material con riflessi.

**Alternativa**: `assets/items/frisbee.fbx` già nel repo (con normali).
Verifica scale e pivot all'import: pivot al centro del disco.

## 4. Scudo

`assets/items/Shield.fbx` – estetica opzionale: attaccalo come child della
mano sx del Player, visibile solo durante la parata (IsBlocking).
Non è collegato alla logica di gioco.

## 5. Ambiente

- Kenney Prototype Kit per le `SideWall` perimetrali (restitution 1.0).
- Skybox/HDRI scuro (Poly Haven) per il vuoto sotto le piattaforme sospese.
- Layout: le due piattaforme si fronteggiano su Z, distanza reciproca
  ~16 m (8 m ciascuna dal centro). Lotto utile per piattaforma: 4 × 2 tile
  (4 m × 2 m); il resto è spazio di gioco per i rimbalzi laterali.

## 6. Audio

`assets/audio/` già contiene wav utilizzabili:

| File | Uso | Campo AudioManager |
|---|---|---|
| glass.wav | futuro: distruggiamo wall | (opzionale) |
| tile_destroy.wav | tile distrutta | `TileDestroy` |
| impact.wav | impatto disco/pareti | `Impact` / OutOfBounds |
| block.wav | parata cattura disco | `Block` |

Da completare (Kenney Audio: https://kenney.nl/assets/interface-sounds):
`Launch`, `HitPlayer`, `Death`, `Victory`, `Gong`.
