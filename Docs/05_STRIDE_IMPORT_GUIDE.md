# Guida Importazione Asset 3D in Stride – Disc Faithful

Ordine consigliato: **personaggi → tile → disco → platform → GameManager → camera → audio → UI**.

## 1. Import FBX

1. Trascina il `.fbx` in `assets/characters/` (o la sottocartella giusta).
2. Game Studio lo converte in `Entity` + `Model + Animations`.
3. Controlla in **Editor → Model**:
   - Scale: il personaggio ~1.8 m; i tile 1 m; il disco Ø 0.36 m
   - Pivot: ai piedi (personaggi), centro (disco, tile)
   - Normale: Y-up (Kenney/Quaternius sono già Y-up)
4. Personaggi: seleziona la **skeleton/Mesh** corretta, aggiungi `AnimationComponent`
   e trascina le clip (`Idle`, `Walk`, `Throw`, …) in `Animations` con i nomi
   usati da `PlayerController` (vedi `03_3D_MODELS_GUIDE.md`).

## 2. Materiali

- Floor: albedo grigio scuro + roughness alta.
- Wall: albedo blu + emissive leggero; i child `Symbole` usano material
  contrasto (bianco/giallo) per essere leggibili a distanza.
- Personaggi: duplica il material e cambia albedo per la CPU (es. rosso).
- Disco: material metallico con riflessi → lo spin è visibile in volo.

## 3. Prefab

1. Crea `DiscPrefab` nel progetto (clic destro → New → Prefab).
2. Entity `DiscRoot`:
   - `RigidbodyComponent`: **Dynamic**, shape sfera r 0.18, massa 0.5
   - Phys material `Phys_Disc`: Restitution 0.98, Friction 0.1
   - `ModelComponent` (cylinder o `frisbee.fbx`)
   - Child `Trail` (TrailComponent, disabilitato di default)
   - Script `Disc`
3. Salva; assegna `DiscPrefab` al campo `GameManager.DiscPrefab`.

## 4. Audio

1. Trascina i `.wav` di `assets/audio/` nel progetto → diventano `Sound` asset.
2. Entity `AudioManager` → script `AudioManager` → trascina i Sound nei campi:
   `Launch`, `Impact`, `TileDestroy`, `HitPlayer`, `Death`, `Victory`, `Gong`, `Block`.
3. NB: un `SoundInstance` per suono; clip corte brevi vadono bene in parallel.

## 5. UI

1. Entity `HUD`: `UIComponent` (AutoLoad=false, la pagina è creata a runtime
   da `GameHud.BuildHud`). Assegna `GameManager` e `Menu` → Entity con `ModeSelectUI`.
2. Entity `Menu`: `UIComponent` **proprio** + script `ModeSelectUI`.
   Font: usare un font arete stile DOS (es. "Press Start 2P", CC0) →
   `UIComponent.Page →` la creazione runtime usa il font default; per customizzare
   crea un `UIPage` asset e assegnalo in editor.
3. Si consiglia rendering: `UIComponent.ResolutionStretch` = Both, res 1920×1080.

## 6. Ciclo di gioco in scena

Checklist finale (vedi `01_STRIDE_SETUP.md` §7):
- Menu → modalità → round auto → round fini → round successivo automatico (3 s)
- Fine tournament/championship → "PRESS A FOR MENU" → ritorno menu
- CPU: si parata, schiva, lancia; sale difficoltà col livello
